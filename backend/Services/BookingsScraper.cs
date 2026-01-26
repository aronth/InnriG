using HtmlAgilityPack;
using InnriGreifi.API.Models.DTOs;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace InnriGreifi.API.Services;

public class BookingsScraper : IBookingsScraper
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BookingsScraper> _logger;
    private readonly IMemoryCache _cache;
    private const string BaseUrl = "http://greifi.stefna.is";
    private const int CacheExpirationMinutes = 10; // Cache for 10 minutes

    public BookingsScraper(HttpClient httpClient, ILogger<BookingsScraper> logger, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _cache = cache;
        
        // Set user agent to match browser requests
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<BookingWeekDto> ScrapeWeekAsync(long unixTimestamp)
    {
        // Normalize timestamp to week start to improve cache hit rate
        // The same week should return the same data regardless of which day's timestamp is used
        var date = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).DateTime;
        var weekStart = date.Date.AddDays(-(int)date.DayOfWeek);
        var normalizedTimestamp = new DateTimeOffset(weekStart).ToUnixTimeSeconds();
        
        // Include cache version in the key to support cache clearing
        var cacheVersion = _cache.GetOrCreate("bookings_cache_version", entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromDays(1);
            return 1;
        });
        
        var cacheKey = $"bookings_week_{normalizedTimestamp}_v{cacheVersion}";
        
        // Try to get from cache first
        if (_cache.TryGetValue<BookingWeekDto>(cacheKey, out var cachedData))
        {
            _logger.LogInformation("Returning cached bookings for week starting {WeekStart} (cache key: {CacheKey})", 
                weekStart.ToString("yyyy-MM-dd"), cacheKey);
            return cachedData!;
        }
        
        try
        {
            var url = $"{BaseUrl}/default/journal/?dt={unixTimestamp}";
            _logger.LogInformation("Scraping bookings from {Url} (cache miss)", url);
            
            // Get bytes and decode with ISO-8859-1 (Latin-1) encoding for Icelandic characters
            var bytes = await _httpClient.GetByteArrayAsync(url);
            var html = Encoding.GetEncoding("ISO-8859-1").GetString(bytes);
            
            var weekData = ParseWeekView(html);
            
            // Cache the result
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes),
                SlidingExpiration = TimeSpan.FromMinutes(CacheExpirationMinutes / 2)
            };
            
            _cache.Set(cacheKey, weekData, cacheOptions);
            _logger.LogInformation("Cached bookings for week starting {WeekStart} for {Minutes} minutes", 
                weekStart.ToString("yyyy-MM-dd"), CacheExpirationMinutes);
            
            return weekData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping bookings for timestamp {Timestamp}", unixTimestamp);
            throw;
        }
    }

    private BookingWeekDto ParseWeekView(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var weekDto = new BookingWeekDto
        {
            Days = new List<BookingDayDto>()
        };

        // Find all day tables
        var dayTables = doc.DocumentNode.SelectNodes("//table[@class='listTable column compact']");
        
        if (dayTables == null || !dayTables.Any())
        {
            _logger.LogWarning("No booking tables found in HTML");
            return weekDto;
        }

        DateTime? firstDate = null;
        DateTime? lastDate = null;

        foreach (var table in dayTables)
        {
            var day = ParseDayTable(table);
            if (day != null)
            {
                weekDto.Days.Add(day);
                
                if (firstDate == null)
                    firstDate = day.Date;
                lastDate = day.Date;
            }
        }

        if (firstDate.HasValue && lastDate.HasValue)
        {
            weekDto.WeekStart = firstDate.Value;
            weekDto.WeekEnd = lastDate.Value;
        }

        return weekDto;
    }

    private BookingDayDto? ParseDayTable(HtmlNode table)
    {
        try
        {
            // Get day header
            var headerLink = table.SelectSingleNode(".//thead//th//a");
            if (headerLink == null)
                return null;

            var dayName = headerLink.InnerText.Trim();
            var dayUrl = headerLink.GetAttributeValue("href", "");
            
            // Extract date from URL parameter (dt=timestamp)
            var dateTimestamp = ExtractTimestampFromUrl(dayUrl);
            var date = dateTimestamp.HasValue 
                ? DateTimeOffset.FromUnixTimeSeconds(dateTimestamp.Value).DateTime 
                : DateTime.UtcNow;

            var dayDto = new BookingDayDto
            {
                Date = date,
                DayName = dayName,
                Bookings = new List<BookingDto>()
            };

            // Get all booking rows (skip the "Ný færsla" row)
            var bookingRows = table.SelectNodes(".//tbody//tr[not(contains(@class, 'bar'))]");
            
            if (bookingRows != null)
            {
                _logger.LogInformation("Found {Count} booking rows for {DayName}", bookingRows.Count, dayName);
                
                foreach (var row in bookingRows)
                {
                    var booking = ParseBookingRow(row, date);
                    if (booking != null)
                    {
                        dayDto.Bookings.Add(booking);
                    }
                }
                
                _logger.LogInformation("Successfully parsed {ParsedCount}/{TotalCount} bookings for {DayName}", 
                    dayDto.Bookings.Count, bookingRows.Count, dayName);
            }

            return dayDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing day table");
            return null;
        }
    }

    private BookingDto? ParseBookingRow(HtmlNode row, DateTime date)
    {
        try
        {
            var td = row.SelectSingleNode(".//td");
            if (td == null)
                return null;

            // Get status from class
            var status = DetermineStatus(row, td);

            // Get start time
            var startTimeNode = td.SelectSingleNode(".//div[@class='startTime']");
            var startTime = startTimeNode?.InnerText.Trim() ?? "";

            // Get booking link
            var linkNode = td.SelectSingleNode(".//a");
            if (linkNode == null)
            {
                _logger.LogWarning("Skipping booking row - no link found. Row HTML: {RowHtml}", row.OuterHtml);
                return null;
            }

            var href = linkNode.GetAttributeValue("href", "");
            var detailUrl = href.StartsWith("http") ? href : $"{BaseUrl}{href}";

            // Parse link text (format: "Short desc.." or just text)
            var linkText = linkNode.InnerText.Trim();
            
            // Get tooltip info
            var onMouseOver = linkNode.GetAttributeValue("onMouseOver", "");
            var tooltipId = ExtractTooltipId(onMouseOver);
            
            string customerName = "";
            string endTime = "";
            int adultCount = 0;
            int childCount = 0;
            string? locationCode = null;

            if (!string.IsNullOrEmpty(tooltipId))
            {
                var tooltipDiv = td.SelectSingleNode($"//div[@id='{tooltipId}']");
                if (tooltipDiv != null)
                {
                    var tooltipData = ParseTooltip(tooltipDiv.InnerHtml);
                    customerName = tooltipData.CustomerName;
                    endTime = tooltipData.EndTime;
                    adultCount = tooltipData.AdultCount;
                    childCount = tooltipData.ChildCount;
                    locationCode = tooltipData.LocationCode;
                }
            }

            // Parse location code and check for print indicator from main text
            var fullText = td.InnerText;
            var needsPrint = fullText.Contains("P -");
            
            // Extract location code from text before the link (e.g., "P - ", "S - ", "L - ")
            // Keep as single letter code if found in the text
            if (string.IsNullOrEmpty(locationCode))
            {
                var locationMatch = Regex.Match(fullText, @"([SPLG])\s*-\s*");
                if (locationMatch.Success)
                {
                    locationCode = locationMatch.Groups[1].Value;
                }
            }

            // Get guest count from the end of the link (e.g., " - 14")
            var guestMatch = Regex.Match(fullText, @"-\s*(\d+)\s*$");
            if (guestMatch.Success && int.TryParse(guestMatch.Groups[1].Value, out var guestCount))
            {
                if (adultCount == 0)
                    adultCount = guestCount;
            }

            return new BookingDto
            {
                Date = date,
                StartTime = startTime,
                EndTime = endTime,
                CustomerName = customerName,
                ShortDescription = linkText,
                AdultCount = adultCount,
                ChildCount = childCount,
                LocationCode = locationCode,
                Status = status,
                DetailUrl = detailUrl,
                NeedsPrint = needsPrint
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing booking row. Row HTML: {RowHtml}", row.OuterHtml);
            return null;
        }
    }

    private string DetermineStatus(HtmlNode row, HtmlNode td)
    {
        var rowClass = row.GetAttributeValue("class", "").ToLower();
        var tdClass = td.GetAttributeValue("class", "").ToLower();
        
        if (rowClass.Contains("confirmed") || tdClass.Contains("confirmed"))
            return "Staðfest";
        if (rowClass.Contains("unconfirmed") || tdClass.Contains("unconfirmed"))
            return "Fyrirspurn";
        if (rowClass.Contains("cancelled") || tdClass.Contains("cancelled"))
            return "Afboðuð";
        
        return "Óþekkt";
    }

    private (string CustomerName, string EndTime, int AdultCount, int ChildCount, string? LocationCode) ParseTooltip(string tooltipHtml)
    {
        // Tooltip format:
        // <strong>Customer Name</strong><br />
        // 08:00 - 09:30 <br /> <br />
        // Location<br />
        // Fjöldi fullorðinna: 1<br />
        // Fjöldi barna: 0
        
        var customerName = "";
        var endTime = "";
        var adultCount = 0;
        var childCount = 0;
        string? locationCode = null;

        // Remove HTML tags for easier parsing
        var text = Regex.Replace(tooltipHtml, "<br\\s*/?>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "<[^>]+>", "");
        
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        
        foreach (var line in lines)
        {
            if (string.IsNullOrEmpty(customerName) && !line.Contains(':') && !line.Contains('-'))
            {
                customerName = line;
            }
            else if (line.Contains(" - ") && line.Contains(":"))
            {
                // Time range (e.g., "08:00 - 09:30")
                var timeParts = line.Split('-', StringSplitOptions.TrimEntries);
                if (timeParts.Length == 2)
                {
                    endTime = timeParts[1].Trim();
                }
            }
            else if (line.StartsWith("Fjöldi fullorðinna:", StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(line, @"(\d+)");
                if (match.Success && int.TryParse(match.Groups[1].Value, out var count))
                {
                    adultCount = count;
                }
            }
            else if (line.StartsWith("Fjöldi barna:", StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(line, @"(\d+)");
                if (match.Success && int.TryParse(match.Groups[1].Value, out var count))
                {
                    childCount = count;
                }
            }
            else if (string.IsNullOrEmpty(locationCode) && !string.IsNullOrWhiteSpace(line))
            {
                // Location line - keep the actual name as-is
                locationCode = line;
            }
        }

        return (customerName, endTime, adultCount, childCount, locationCode);
    }

    private string? ExtractTooltipId(string onMouseOverAttribute)
    {
        // Format: javascript:sh('tooltip_46018', 'tooltip');
        var match = Regex.Match(onMouseOverAttribute, @"tooltip_(\d+)");
        return match.Success ? $"tooltip_{match.Groups[1].Value}" : null;
    }

    private long? ExtractTimestampFromUrl(string url)
    {
        // Extract dt parameter from URL
        var match = Regex.Match(url, @"[?&]dt=(\d+)");
        if (match.Success && long.TryParse(match.Groups[1].Value, out var timestamp))
        {
            return timestamp;
        }
        return null;
    }
}

