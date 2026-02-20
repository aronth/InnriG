using System.Text.Json;
using InnriGreifi.API.Models;
using Microsoft.EntityFrameworkCore;

namespace InnriGreifi.API.Services.Steps;

public class BookingAvailabilityCheckStepHandler : IWorkflowStepHandler
{
    private readonly IBookingManagementService _bookingService;
    private readonly ILogger<BookingAvailabilityCheckStepHandler> _logger;

    public BookingAvailabilityCheckStepHandler(
        IBookingManagementService bookingService,
        ILogger<BookingAvailabilityCheckStepHandler> logger)
    {
        _bookingService = bookingService;
        _logger = logger;
    }

    public async Task<WorkflowStepResult> ExecuteAsync(
        WorkflowInstance workflow,
        EmailConversation conversation,
        EmailExtractedData? extractedData,
        Dictionary<string, object> configuration,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("BookingAvailabilityCheckStepHandler: Checking availability for workflow {WorkflowInstanceId}", workflow.Id);

            // Get requested date from extracted data
            DateTime? requestedDate = extractedData?.RequestedDate;
            
            if (!requestedDate.HasValue)
            {
                // Try to get from workflow data (in case it was set in a previous step)
                var workflowData = DeserializeWorkflowData(workflow.WorkflowDataJson);
                if (workflowData.TryGetValue("RequestedDate", out var dateObj))
                {
                    var dateStr = ExtractString(dateObj);
                    if (DateTime.TryParse(dateStr, out var parsedDate))
                    {
                        requestedDate = parsedDate;
                    }
                }
            }

            if (!requestedDate.HasValue)
            {
                return new WorkflowStepResult
                {
                    Success = false,
                    ErrorMessage = "No booking date found in extracted data"
                };
            }

            // Get bookings for the requested date (only times and guest counts, no names/phones)
            var fromDate = requestedDate.Value.Date;
            var toDate = fromDate.AddDays(1).AddTicks(-1); // End of the same day

            var bookings = await _bookingService.GetBookingsAsync(
                fromDate: fromDate,
                toDate: toDate,
                customerId: null,
                locationId: null,
                status: null);

            // Format bookings for AI (only time and guest count)
            var bookingInfo = bookings.Select(b => new Dictionary<string, object>
            {
                ["Time"] = b.StartTime.ToString(@"hh\:mm"),
                ["GuestCount"] = b.AdultCount + b.ChildCount
            }).ToList();

            return new WorkflowStepResult
            {
                Success = true,
                OutputData = new Dictionary<string, object>
                {
                    ["RequestedDate"] = requestedDate.Value.ToString("yyyy-MM-dd"),
                    ["ExistingBookings"] = JsonSerializer.Serialize(bookingInfo),
                    ["BookingCount"] = bookingInfo.Count
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in BookingAvailabilityCheckStepHandler for workflow {WorkflowInstanceId}", workflow.Id);
            return new WorkflowStepResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private Dictionary<string, object> DeserializeWorkflowData(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return new Dictionary<string, object>();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }

    private string? ExtractString(object? value)
    {
        if (value == null) return null;
        if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.String)
            return jsonElement.GetString();
        if (value is string str) return str;
        return value.ToString();
    }
}

