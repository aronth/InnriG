using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace InnriGreifi.API.Services;

public static class OrderTextParser
{
    private static readonly CultureInfo IsCulture = CultureInfo.GetCultureInfo("is-IS");

    // Matches e.g. "01.12.2025 15:27:46" or "1.12.2025 17:51"
    private const string DateTimePattern = @"\d{1,2}[./]\d{1,2}[./]\d{4}\s+\d{1,2}:\d{2}(?::\d{2})?";

    private static readonly Regex DateTimeRegex = new(
        $@"(?<dt>{DateTimePattern})",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static (DateTime? scannedAtUtc, DateTime? checkedOutAtUtc) ParseReikningstexti3(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return (null, null);

        var text = raw.Trim();
        text = text.Replace("\r\n", "\n").Replace('\r', '\n');

        // First: try keyword-anchored parsing (handles "Skannað kl. ..." and similar)
        var (scanByKeyword, checkoutByKeyword) = ParseByKeywords(text);
        if (scanByKeyword != null || checkoutByKeyword != null)
        {
            // Fill gaps using positional fallback if we can clearly see 1-2 timestamps
            var keywordMatches = DateTimeRegex.Matches(text);
            var keywordFirst = keywordMatches.Count >= 1 ? TryParseDateTime(keywordMatches[0].Groups["dt"].Value) : null;
            var keywordSecond = keywordMatches.Count >= 2 ? TryParseDateTime(keywordMatches[1].Groups["dt"].Value) : null;

            return (
                scanByKeyword ?? keywordFirst,
                checkoutByKeyword ?? keywordSecond
            );
        }

        // Fallback: extract up to two datetimes; assume first=scan, second=checkout
        var dtMatches = DateTimeRegex.Matches(text);
        if (dtMatches.Count == 0)
            return (null, null);

        DateTime? first = TryParseDateTime(dtMatches[0].Groups["dt"].Value);
        DateTime? second = dtMatches.Count >= 2 ? TryParseDateTime(dtMatches[1].Groups["dt"].Value) : null;

        return (first, second);
    }

    private static (DateTime? scannedAtUtc, DateTime? checkedOutAtUtc) ParseByKeywords(string text)
    {
        var normalized = NormalizeAscii(text);

        // Avoid cross-line/cross-event matching by scanning per line first.
        DateTime? scan = null;
        DateTime? checkout = null;

        var scanPatterns = new[]
        {
            $@"skann[a-z]*\s*(kl\.?\s*)?(?<dt>{DateTimePattern})",
            $@"(?<dt>{DateTimePattern})\s*skann[a-z]*"
        };

        var checkoutPatterns = new[]
        {
            $@"skrad\s*ut\s*(kl\.?\s*)?(?<dt>{DateTimePattern})",
            $@"(?<dt>{DateTimePattern})\s*skrad\s*ut"
        };

        foreach (var line in normalized.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (scan == null)
            {
                var scanDtStr = TryGetDateTimeStringByPatterns(line, scanPatterns);
                scan = scanDtStr != null ? TryParseDateTime(scanDtStr) : null;
            }

            if (checkout == null)
            {
                var checkoutDtStr = TryGetDateTimeStringByPatterns(line, checkoutPatterns);
                checkout = checkoutDtStr != null ? TryParseDateTime(checkoutDtStr) : null;
            }
        }

        // Still missing? Try whole text (handles "Skannað ... Skráð út ..." on a single line)
        if (scan == null)
        {
            var scanDtStr = TryGetDateTimeStringByPatterns(normalized, scanPatterns);
            scan = scanDtStr != null ? TryParseDateTime(scanDtStr) : null;
        }

        if (checkout == null)
        {
            var checkoutDtStr = TryGetDateTimeStringByPatterns(normalized, checkoutPatterns);
            checkout = checkoutDtStr != null ? TryParseDateTime(checkoutDtStr) : null;
        }

        // If we only found a checkout timestamp, treat it as scan when it's the only signal we have
        if (scan == null && checkout != null && normalized.Contains("skann") == false)
            scan = checkout;

        return (scan, checkout);
    }

    private static string? TryGetDateTimeStringByPatterns(string normalizedText, IEnumerable<string> patterns)
    {
        foreach (var pattern in patterns)
        {
            var rx = new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
            var m = rx.Match(normalizedText);
            if (m.Success)
                return m.Groups["dt"].Value;
        }

        return null;
    }

    private static DateTime? TryParseDateTime(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var s = value.Trim();

        var formats = new[]
        {
            "d.M.yyyy H:mm:ss",
            "d.M.yyyy H:mm",
            "dd.MM.yyyy HH:mm:ss",
            "dd.MM.yyyy HH:mm",
            "d.M.yyyy HH:mm:ss",
            "d.M.yyyy HH:mm"
        };

        if (DateTime.TryParseExact(s, formats, IsCulture, DateTimeStyles.AllowWhiteSpaces, out var dt) ||
            DateTime.TryParse(s, IsCulture, DateTimeStyles.AllowWhiteSpaces, out dt))
        {
            // Iceland is UTC year-round; treat unspecified as UTC to keep storage consistent
            if (dt.Kind == DateTimeKind.Unspecified)
                dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            else if (dt.Kind == DateTimeKind.Local)
                dt = dt.ToUniversalTime();

            return dt;
        }

        return null;
    }

    private static string NormalizeAscii(string input)
    {
        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc == UnicodeCategory.NonSpacingMark)
                continue;
            sb.Append(char.ToLowerInvariant(ch));
        }

        // Normalize whitespace a bit
        return sb.ToString().Replace('\u00A0', ' ');
    }
}


