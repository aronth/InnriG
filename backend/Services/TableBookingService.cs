using HtmlAgilityPack;
using InnriGreifi.API.Models.DTOs;
using System.Globalization;
using System.Text;

namespace InnriGreifi.API.Services;

public class TableBookingService : ITableBookingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TableBookingService> _logger;
    private const string BaseUrl = "https://www.greifinn.is/is/bordapontun/booking";

    public TableBookingService(HttpClient httpClient, ILogger<TableBookingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Set user agent to match browser requests
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
    }

    public async Task<TableBookingListDto> GetBookingsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? contactName = null,
        string? contactPhone = null,
        int? statusId = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = BuildUrl(fromDate, toDate, contactName, contactPhone, statusId, page, pageSize);

            _logger.LogInformation("Scraping table bookings from {Url}", url);

            // Get bytes and decode with UTF-8 encoding
            var bytes = await _httpClient.GetByteArrayAsync(url, cancellationToken);
            var html = Encoding.UTF8.GetString(bytes);

            var (bookings, totalCount) = ParseBookings(html);
            
            // Determine if there are more pages
            // For single day queries, itemCount=-1 gets all results, so no pagination
            var isSingleDay = fromDate.HasValue && toDate.HasValue &&
                              toDate.Value.Date == fromDate.Value.Date.AddDays(1);
            
            // For single day, no pagination (itemCount=-1 gets all)
            // For multi-day, if we got fewer bookings than the total count, there are more pages
            var hasMorePages = !isSingleDay && totalCount > 0 && bookings.Count > 0 && totalCount > bookings.Count;

            return new TableBookingListDto
            {
                Bookings = bookings,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                HasMorePages = hasMorePages
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping table bookings from Greifinn");
            throw;
        }
    }

    private string BuildUrl(
        DateTime? fromDate,
        DateTime? toDate,
        string? contactName,
        string? contactPhone,
        int? statusId,
        int page,
        int pageSize)
    {
        var queryParams = new List<string>();

        if (fromDate.HasValue)
        {
            // Format as Icelandic date: dd.MM.yyyy
            // Note: The user mentioned that toDate should be the next day to get only the requested date
            var fromDateStr = fromDate.Value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
            queryParams.Add($"flt-timestamp%5Bfrom%5D={Uri.EscapeDataString(fromDateStr)}");
        }

        if (toDate.HasValue)
        {
            // Format as Icelandic date: dd.MM.yyyy
            var toDateStr = toDate.Value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
            queryParams.Add($"flt-timestamp%5Bto%5D={Uri.EscapeDataString(toDateStr)}");
        }
        else if (fromDate.HasValue)
        {
            // If fromDate is provided but toDate is not, set toDate to next day (as per user's note)
            var nextDay = fromDate.Value.AddDays(1);
            var toDateStr = nextDay.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
            queryParams.Add($"flt-timestamp%5Bto%5D={Uri.EscapeDataString(toDateStr)}");
        }

        if (!string.IsNullOrWhiteSpace(contactName))
            queryParams.Add($"flt-contactName={Uri.EscapeDataString(contactName)}");
        else
            queryParams.Add("flt-contactName=");

        if (!string.IsNullOrWhiteSpace(contactPhone))
            queryParams.Add($"flt-contactPhone={Uri.EscapeDataString(contactPhone)}");
        else
            queryParams.Add("flt-contactPhone=");

        if (statusId.HasValue)
            queryParams.Add($"flt-status={statusId.Value}");
        else
            queryParams.Add("flt-status=-1");

        // Determine if this is a single day query
        // The system handles single day by setting fromDate to the day and toDate to the next day
        var isSingleDay = fromDate.HasValue && toDate.HasValue &&
                          toDate.Value.Date == fromDate.Value.Date.AddDays(1);

        if (isSingleDay)
        {
            // For single day, use itemCount=-1 to get all bookings
            queryParams.Add("itemCount=-1");
        }
        else
        {
            // For multi-day or pagination, use page and itemCount=-1
            queryParams.Add($"page={page}");
            queryParams.Add("itemCount=-1");
        }

        var queryString = string.Join("&", queryParams);
        return $"{BaseUrl}?{queryString}";
    }

    private (List<TableBookingDto> Bookings, int TotalCount) ParseBookings(string html)
    {
        var bookings = new List<TableBookingDto>();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Extract total count from pagination info (if available)
        // Format: "101 Færslur" (101 Records)
        var totalCount = 0;
        var paginatorNode = doc.DocumentNode.SelectSingleNode("//div[@class='paginator']");
        if (paginatorNode != null)
        {
            var paginatorText = paginatorNode.InnerText;
            var countMatch = System.Text.RegularExpressions.Regex.Match(paginatorText, @"(\d+)\s+Færslur");
            if (countMatch.Success && int.TryParse(countMatch.Groups[1].Value, out var parsedCount))
            {
                totalCount = parsedCount;
            }
        }

        // Find booking rows in the table body (rows with class altRow1 or altRow2, similar to orders)
        var bookingRows = doc.DocumentNode.SelectNodes("//table//tbody//tr[contains(@class, 'altRow')]");

        if (bookingRows != null)
        {
            foreach (var row in bookingRows)
            {
                var booking = ParseBookingRow(row);
                if (booking != null)
                {
                    bookings.Add(booking);
                }
            }
        }
        else
        {
            _logger.LogWarning("Could not find booking rows in HTML. Expected table rows with class 'altRow1' or 'altRow2'.");
        }

        // If we couldn't parse total count, use the number of bookings found
        if (totalCount == 0)
        {
            totalCount = bookings.Count;
        }

        _logger.LogInformation("Parsed {Count} bookings from HTML (total: {TotalCount})", bookings.Count, totalCount);
        return (bookings, totalCount);
    }

    private TableBookingDto? ParseBookingRow(HtmlNode row)
    {
        try
        {
            var cells = row.SelectNodes(".//td");
            if (cells == null || cells.Count < 6)
            {
                _logger.LogWarning("Booking row has insufficient cells. Expected at least 6, found {Count}", cells?.Count ?? 0);
                return null;
            }

            var booking = new TableBookingDto();

            // Extract booking ID and detail URL from the link in the last column (toolCol)
            // Link format: /is/bordapontun/booking/edit/{bookingId} where bookingId is an integer
            // The user confirmed: "the id is not a guid, it is an int. It can be found in the url at the end of table row"
            // The link is inside a hidden dropdown menu: ul.hidden li a
            // Structure: <td class="toolCol last"><div class="jip"><a href="#">...</a><ul class="hidden"><li><a href="/is/bordapontun/booking/edit/47127">...</a></li></ul></div></td>
            
            // First, try to find the link in the hidden dropdown menu (most reliable)
            // The ul might have class="hidden" or contain "hidden" in its class attribute
            var hiddenUl = row.SelectNodes(".//ul[contains(@class, 'hidden')]");
            if (hiddenUl != null && hiddenUl.Count > 0)
            {
                foreach (var ul in hiddenUl)
                {
                    var links = ul.SelectNodes(".//li//a[@href]");
                    if (links != null)
                    {
                        foreach (var link in links)
                        {
                            var href = link.GetAttributeValue("href", "");
                            if (!string.IsNullOrEmpty(href) && href != "#" && href.Contains("/booking/edit/"))
                            {
                                // Normalize href - remove query strings and fragments
                                var cleanHref = href.Split('?')[0].Split('#')[0];
                                
                                booking.DetailUrl = cleanHref.StartsWith("http") ? cleanHref : $"https://www.greifinn.is{cleanHref}";
                                
                                // Extract booking ID (integer) from URL: /is/bordapontun/booking/edit/47127
                                var bookingIdMatch = System.Text.RegularExpressions.Regex.Match(cleanHref, @"/booking/edit/(\d+)");
                                if (bookingIdMatch.Success)
                                {
                                    booking.BookingId = bookingIdMatch.Groups[1].Value;
                                    _logger.LogDebug("Extracted booking ID {BookingId} from hidden dropdown link: {Url}", booking.BookingId, cleanHref);
                                    break; // Found it, no need to continue
                                }
                            }
                        }
                        if (!string.IsNullOrEmpty(booking.BookingId))
                        {
                            break; // Found it, no need to check other ul elements
                        }
                    }
                }
            }
            
            // Alternative: try direct XPath with exact class match
            if (string.IsNullOrEmpty(booking.BookingId))
            {
                var hiddenLink = row.SelectSingleNode(".//ul[@class='hidden']//li//a[@href]");
                if (hiddenLink != null)
                {
                    var href = hiddenLink.GetAttributeValue("href", "");
                    if (!string.IsNullOrEmpty(href) && href != "#" && href.Contains("/booking/edit/"))
                    {
                        var cleanHref = href.Split('?')[0].Split('#')[0];
                        booking.DetailUrl = cleanHref.StartsWith("http") ? cleanHref : $"https://www.greifinn.is{cleanHref}";
                        
                        var bookingIdMatch = System.Text.RegularExpressions.Regex.Match(cleanHref, @"/booking/edit/(\d+)");
                        if (bookingIdMatch.Success)
                        {
                            booking.BookingId = bookingIdMatch.Groups[1].Value;
                            _logger.LogDebug("Extracted booking ID {BookingId} from hidden dropdown link (exact match): {Url}", booking.BookingId, cleanHref);
                        }
                    }
                }
            }
            
            // Fallback: try to find any link with /booking/edit/ pattern in the last cell
            if (string.IsNullOrEmpty(booking.BookingId))
            {
                var lastCell = row.SelectSingleNode(".//td[last()]");
                if (lastCell != null)
                {
                    // Look for any link containing /booking/edit/ in the last cell
                    var allLinks = lastCell.SelectNodes(".//a[@href]");
                    if (allLinks != null)
                    {
                        foreach (var link in allLinks)
                        {
                            var href = link.GetAttributeValue("href", "");
                            if (!string.IsNullOrEmpty(href) && href != "#" && href.Contains("/booking/edit/"))
                            {
                                var cleanHref = href.Split('?')[0].Split('#')[0];
                                booking.DetailUrl = cleanHref.StartsWith("http") ? cleanHref : $"https://www.greifinn.is{cleanHref}";
                                
                                var bookingIdMatch = System.Text.RegularExpressions.Regex.Match(cleanHref, @"/booking/edit/(\d+)");
                                if (bookingIdMatch.Success)
                                {
                                    booking.BookingId = bookingIdMatch.Groups[1].Value;
                                    _logger.LogDebug("Extracted booking ID {BookingId} from link in last cell: {Url}", booking.BookingId, cleanHref);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            
            // If we have a booking ID but no detail URL, construct it
            if (!string.IsNullOrEmpty(booking.BookingId) && string.IsNullOrEmpty(booking.DetailUrl))
            {
                booking.DetailUrl = $"https://www.greifinn.is/is/bordapontun/booking/edit/{booking.BookingId}";
            }
            
            // Log warning if we still don't have a booking ID
            if (string.IsNullOrEmpty(booking.BookingId))
            {
                _logger.LogWarning("Could not extract booking ID from row. Row HTML: {RowHtml}", row.OuterHtml.Substring(0, Math.Min(500, row.OuterHtml.Length)));
            }

            // Based on actual HTML structure analysis:
            // Cell 0: Date (dd.MM.yyyy) - class="thin first"
            // Cell 1: Time (HH:mm) - class="thin"
            // Cell 2: Contact Name - class="text-nowrap"
            // Cell 3: Guest Count (Fjöldi) - class="thin"
            // Cell 4: Gestir skráðir (registered guests, can be empty) - class="thin text-center"
            // Cell 5: Phone - class="thin"
            // Cell 6: Notes/Athugasemd - class="thin text-center notes"
            // Cell 7: Status (Ný/Situr/Afbókað/Farinn) - class="toolCol"
            // Cell 8: Tool column (with link) - class="toolCol last"

            // Log cell contents for debugging
            _logger.LogDebug("Parsing booking row with {Count} cells", cells.Count);
            for (int i = 0; i < Math.Min(cells.Count, 9); i++)
            {
                _logger.LogDebug("Cell {Index}: '{Content}' (class: '{Class}')", 
                    i, cells[i].InnerText.Trim(), cells[i].GetAttributeValue("class", ""));
            }

            // Cell 0: Date (dd.MM.yyyy)
            DateTime? date = null;
            if (cells.Count > 0)
            {
                var dateStr = cells[0].InnerText.Trim();
                if (!string.IsNullOrEmpty(dateStr) && DateTime.TryParseExact(dateStr, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                {
                    date = parsedDate;
                    _logger.LogDebug("Found date '{Date}' in cell 0", dateStr);
                }
            }

            // Cell 1: Time (HH:mm)
            TimeSpan? time = null;
            if (cells.Count > 1)
            {
                var timeStr = cells[1].InnerText.Trim();
                if (!string.IsNullOrEmpty(timeStr) && TimeSpan.TryParseExact(timeStr, "hh\\:mm", CultureInfo.InvariantCulture, out var parsedTime))
                {
                    time = parsedTime;
                    _logger.LogDebug("Found time '{Time}' in cell 1", timeStr);
                }
            }

            // Combine date and time to create timestamp
            if (date.HasValue)
            {
                if (time.HasValue)
                {
                    booking.Timestamp = date.Value.Date.Add(time.Value);
                }
                else
                {
                    booking.Timestamp = date.Value;
                }
            }

            // Cell 2: Contact Name
            if (cells.Count > 2)
            {
                booking.ContactName = cells[2].InnerText.Trim();
            }

            // Cell 3: Guest Count (Fjöldi)
            if (cells.Count > 3)
            {
                var guestCountStr = cells[3].InnerText.Trim();
                if (!string.IsNullOrEmpty(guestCountStr) && int.TryParse(guestCountStr, out var guestCount))
                {
                    booking.GuestCount = guestCount;
                }
            }

            // Cell 4: Gestir skráðir (registered guests) - can be empty, skip for now

            // Cell 5: Phone
            if (cells.Count > 5)
            {
                booking.ContactPhone = cells[5].InnerText.Trim();
            }

            // Cell 6: Notes/Athugasemd - check if empty
            if (cells.Count > 6)
            {
                var notesText = cells[6].InnerText.Trim();
                booking.HasComment = !string.IsNullOrEmpty(notesText);
                _logger.LogDebug("Notes cell (index 6): '{Content}', HasComment={HasComment}", notesText, booking.HasComment);
            }

            // Cell 7: Status (Ný/Situr/Afbókað/Farinn)
            if (cells.Count > 7)
            {
                var statusText = cells[7].InnerText.Trim();
                if (!string.IsNullOrEmpty(statusText))
                {
                    // Validate that it's one of the expected status values
                    if (statusText.Equals("Ný", StringComparison.OrdinalIgnoreCase) ||
                        statusText.Equals("Situr", StringComparison.OrdinalIgnoreCase) ||
                        statusText.Equals("Afbókað", StringComparison.OrdinalIgnoreCase) ||
                        statusText.Equals("Farinn", StringComparison.OrdinalIgnoreCase))
                    {
                        booking.Status = statusText;
                        _logger.LogDebug("Found status '{Status}' in cell 7", statusText);
                    }
                }
            }

            // If status is still empty, search all cells (except last tool column) for status values
            if (string.IsNullOrEmpty(booking.Status))
            {
                for (int i = 0; i < cells.Count - 1; i++) // Exclude last cell (tool column)
                {
                    var cellText = cells[i].InnerText.Trim();
                    if (!string.IsNullOrEmpty(cellText))
                    {
                        // Check if this cell contains a status value
                        if (cellText.Equals("Ný", StringComparison.OrdinalIgnoreCase) ||
                            cellText.Equals("Situr", StringComparison.OrdinalIgnoreCase) ||
                            cellText.Equals("Afbókað", StringComparison.OrdinalIgnoreCase) ||
                            cellText.Equals("Farinn", StringComparison.OrdinalIgnoreCase))
                        {
                            booking.Status = cellText;
                            _logger.LogDebug("Found status '{Status}' in cell {Index}", cellText, i);
                            break;
                        }
                    }
                }
            }

            // Don't generate a GUID fallback - booking ID should always be an integer from the URL
            // If we don't have it, log it and leave it empty (the frontend can handle null/empty)
            if (string.IsNullOrEmpty(booking.BookingId))
            {
                _logger.LogWarning("Booking ID is still empty after all extraction attempts. Contact: {ContactName}, Phone: {Phone}", 
                    booking.ContactName, booking.ContactPhone);
            }

            return booking;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing booking row. HTML: {RowHtml}", row.OuterHtml);
            return null;
        }
    }
}


