using System.Text.Json;
using InnriGreifi.API.Models;
using InnriGreifi.API.Models.DTOs;

namespace InnriGreifi.API.Services.Steps;

public class OrderLookupStepHandler : IWorkflowStepHandler
{
    private readonly IGreifinnOrderScraper _orderScraper;
    private readonly ILogger<OrderLookupStepHandler> _logger;

    public OrderLookupStepHandler(
        IGreifinnOrderScraper orderScraper,
        ILogger<OrderLookupStepHandler> logger)
    {
        _orderScraper = orderScraper;
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
            _logger.LogInformation("OrderLookupStepHandler: Looking up orders for conversation {ConversationId}", conversation.Id);

            if (extractedData == null)
            {
                return new WorkflowStepResult
                {
                    Success = false,
                    ErrorMessage = "No extracted data available"
                };
            }

            var phone = extractedData.ContactPhone;
            var date = extractedData.RequestedDate;

            if (string.IsNullOrEmpty(phone) && !date.HasValue)
            {
                return new WorkflowStepResult
                {
                    Success = false,
                    ErrorMessage = "Phone number and date are required for order lookup"
                };
            }

            // Lookup orders
            var fromDate = date ?? DateTime.UtcNow.AddDays(-7);
            var toDate = date?.AddDays(1) ?? DateTime.UtcNow;

            var ordersResult = await _orderScraper.GetOrdersAsync(
                phoneNumber: phone,
                fromDate: fromDate,
                toDate: toDate,
                page: 1,
                pageSize: 50,
                cancellationToken: ct);

            _logger.LogInformation("OrderLookupStepHandler: Found {Count} orders for phone {Phone} on date {Date}",
                ordersResult.Orders.Count, phone, date);

            return new WorkflowStepResult
            {
                Success = true,
                OutputData = new Dictionary<string, object>
                {
                    ["MatchedOrders"] = ordersResult.Orders, // Store as list directly, not as JSON string
                    ["MatchCount"] = ordersResult.Orders.Count,
                    ["SearchPhone"] = phone ?? string.Empty,
                    ["SearchDate"] = date?.ToString("yyyy-MM-dd") ?? string.Empty
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OrderLookupStepHandler for workflow {WorkflowInstanceId}", workflow.Id);
            return new WorkflowStepResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

