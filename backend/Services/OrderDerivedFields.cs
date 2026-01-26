using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace InnriGreifi.API.Services;

public static class OrderDerivedFields
{
    private static readonly Regex DateTimeRegex = new(
        @"\d{1,2}[./]\d{1,2}[./]\d{4}\s+\d{1,2}:\d{2}(?::\d{2})?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string ComputeDeliveryMethod(string? orderType, string? invoiceText3Raw, string? orderNumber = null)
    {
        // 0) Highest priority: If Pantananúmer is 0, then method is "Lausa sala"
        if (!string.IsNullOrWhiteSpace(orderNumber))
        {
            var digits = new string(orderNumber.Trim().Where(char.IsDigit).ToArray());
            if (int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n == 0)
            {
                return "Lausa sala";
            }
        }

        // 1) Primary: Tegund pöntunar
        var methodFromOrderType = MapDeliveryMethod(orderType);
        if (methodFromOrderType != "Unknown")
            return methodFromOrderType;

        // 2) Secondary: Reikningstexti 3 (only certain known values)
        if (!string.IsNullOrWhiteSpace(invoiceText3Raw))
        {
            var lines = invoiceText3Raw
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var line in lines)
            {
                // Ignore lines that are obviously timestamps
                if (DateTimeRegex.IsMatch(line))
                    continue;

                var m = MapDeliveryMethod(line);
                if (m != "Unknown")
                    return m;
            }
        }

        return "Unknown";
    }

    public static string ComputeOrderSource(string? orderNumber)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            return "Unknown";

        var digits = new string(orderNumber.Trim().Where(char.IsDigit).ToArray());
        if (!int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
            return "Unknown";

        return n < 1000 ? "Counter" : "Web";
    }

    public static (TimeOnly? orderTime, TimeOnly? readyTime, int? waitTimeMin) ComputeTimes(DateTime? createdDateUtc, DateTime? deliveryDateUtc)
    {
        TimeOnly? orderTime = createdDateUtc != null ? TimeOnly.FromDateTime(createdDateUtc.Value) : null;
        TimeOnly? readyTime = deliveryDateUtc != null ? TimeOnly.FromDateTime(deliveryDateUtc.Value) : null;

        int? waitMin = null;
        if (createdDateUtc != null && deliveryDateUtc != null)
        {
            var delta = deliveryDateUtc.Value - createdDateUtc.Value;
            var minutes = (int)Math.Round(delta.TotalMinutes, MidpointRounding.AwayFromZero);

            if (minutes >= 0)
            {
                // Round to nearest 5 minutes (as requested)
                waitMin = (int)(Math.Round(minutes / 5.0, MidpointRounding.AwayFromZero) * 5);
            }
        }

        return (orderTime, readyTime, waitMin);
    }

    private static string MapDeliveryMethod(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Unknown";

        var norm = Normalize(value);

        // Values we see in the excel / text:
        // - "Sótt" / "Take away" => Sótt
        // - "Heimsent" => Sent
        // - "Í sal" / "Salur" => Salur
        if (norm.Contains("heimsent", StringComparison.Ordinal))
            return "Sent";

        if (norm.Contains("sott", StringComparison.Ordinal) || norm.Contains("take away", StringComparison.Ordinal))
            return "Sótt";

        if (norm.Contains("i sal", StringComparison.Ordinal) || norm.Equals("salur", StringComparison.Ordinal) || norm.Contains("salur", StringComparison.Ordinal))
            return "Salur";

        return "Unknown";
    }

    private static string Normalize(string input)
    {
        var s = input.Trim().ToLowerInvariant();
        var normalized = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc == UnicodeCategory.NonSpacingMark)
                continue;
            sb.Append(ch);
        }

        return sb.ToString().Replace('\u00A0', ' ');
    }
}


