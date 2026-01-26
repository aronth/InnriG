using InnriGreifi.API.Models;
using Microsoft.Extensions.Options;

namespace InnriGreifi.API.Services;

public class OrderLateRules
{
    private readonly OrderLateRulesOptions _options;

    public OrderLateRules(IOptions<OrderLateRulesOptions> options)
    {
        _options = options.Value;
    }

    public bool IsEvaluable(string method, DateTime? checkedOutAt, DateTime? scannedAt)
    {
        return method == "Sent" ? checkedOutAt != null : scannedAt != null;
    }

    /// <summary>
    /// Per user: late if "minutes left" is less than threshold.
    /// Sent: (ReadyDateTime - CheckedOutAt) < configured threshold minutes
    /// Other: (ReadyDateTime - ScannedAt) < configured threshold minutes
    /// ReadyDateTime is calculated from DeliveryDate.Date + ReadyTime
    /// </summary>
    public bool IsLate(string method, DateTime deliveryDateUtc, DateTime? checkedOutAtUtc, DateTime? scannedAtUtc, TimeOnly? readyTime = null)
    {
        // Combine DeliveryDate with ReadyTime to get the actual ready datetime
        DateTime readyDateTimeUtc;
        if (readyTime.HasValue)
        {
            // Use the date from DeliveryDate but the time from ReadyTime
            readyDateTimeUtc = new DateTime(
                deliveryDateUtc.Year,
                deliveryDateUtc.Month,
                deliveryDateUtc.Day,
                readyTime.Value.Hour,
                readyTime.Value.Minute,
                0,
                DateTimeKind.Utc);
        }
        else
        {
            // Fallback to using DeliveryDate as-is (for backward compatibility)
            readyDateTimeUtc = deliveryDateUtc;
        }

        if (method == "Sent")
        {
            if (checkedOutAtUtc == null) return false;
            return (readyDateTimeUtc - checkedOutAtUtc.Value).TotalMinutes < _options.SentThresholdMinutes;
        }

        if (scannedAtUtc == null) return false;
        return (readyDateTimeUtc - scannedAtUtc.Value).TotalMinutes < _options.OtherThresholdMinutes;
    }
}


