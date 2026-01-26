using HtmlAgilityPack;
using InnriGreifi.API.Models.DTOs;
using System.Globalization;
using System.Text;

namespace InnriGreifi.API.Services;

public class GreifinnOrderScraper : IGreifinnOrderScraper
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GreifinnOrderScraper> _logger;
    private const string BaseUrl = "https://www.greifinn.is/is/moya/pilot/order";

    public GreifinnOrderScraper(HttpClient httpClient, ILogger<GreifinnOrderScraper> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Set user agent to match browser requests
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
    }

    public async Task<GreifinnOrderListDto> GetOrdersAsync(
        string? phoneNumber = null,
        string? customerName = null,
        string? customerAddress = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? locationId = null,
        int? deliveryMethodId = null,
        int? paymentMethodId = null,
        decimal? totalPrice = null,
        string? externalId = null,
        int? statusId = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = BuildUrl(
                phoneNumber, customerName, customerAddress,
                fromDate, toDate, locationId, deliveryMethodId,
                paymentMethodId, totalPrice, externalId, statusId,
                page, pageSize);

            _logger.LogInformation("Scraping orders from {Url}", url);

            // Get bytes and decode with UTF-8 encoding
            var bytes = await _httpClient.GetByteArrayAsync(url, cancellationToken);
            var html = Encoding.UTF8.GetString(bytes);

            var (orders, totalCount) = ParseOrders(html);
            
            // Determine if there are more pages
            var hasMorePages = totalCount > (page * pageSize);

            return new GreifinnOrderListDto
            {
                Orders = orders,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                HasMorePages = hasMorePages
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping orders from Greifinn");
            throw;
        }
    }

    private string BuildUrl(
        string? phoneNumber,
        string? customerName,
        string? customerAddress,
        DateTime? fromDate,
        DateTime? toDate,
        int? locationId,
        int? deliveryMethodId,
        int? paymentMethodId,
        decimal? totalPrice,
        string? externalId,
        int? statusId,
        int page,
        int pageSize)
    {
        var queryParams = new List<string>();

        if (!string.IsNullOrWhiteSpace(phoneNumber))
            queryParams.Add($"flt-phone={Uri.EscapeDataString(phoneNumber)}");

        if (!string.IsNullOrWhiteSpace(customerName))
            queryParams.Add($"flt-customerName={Uri.EscapeDataString(customerName)}");

        if (!string.IsNullOrWhiteSpace(customerAddress))
            queryParams.Add($"flt-customerAddress={Uri.EscapeDataString(customerAddress)}");

        if (fromDate.HasValue)
        {
            // Format as Icelandic date: dd.MM.yyyy
            var fromDateStr = fromDate.Value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
            queryParams.Add($"flt-added%5Bfrom%5D={Uri.EscapeDataString(fromDateStr)}");
        }

        if (toDate.HasValue)
        {
            // Format as Icelandic date: dd.MM.yyyy
            var toDateStr = toDate.Value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
            queryParams.Add($"flt-added%5Bto%5D={Uri.EscapeDataString(toDateStr)}");
        }

        if (locationId.HasValue)
            queryParams.Add($"flt-locationId={locationId.Value}");
        else
            queryParams.Add("flt-locationId=-1");

        if (deliveryMethodId.HasValue)
            queryParams.Add($"flt-deliveryMethodId={deliveryMethodId.Value}");
        else
            queryParams.Add("flt-deliveryMethodId=-1");

        if (paymentMethodId.HasValue)
            queryParams.Add($"flt-paymentMethodId={paymentMethodId.Value}");
        else
            queryParams.Add("flt-paymentMethodId=-1");

        if (totalPrice.HasValue)
            queryParams.Add($"flt-totalPrice={totalPrice.Value}");

        if (!string.IsNullOrWhiteSpace(externalId))
            queryParams.Add($"flt-externalId={Uri.EscapeDataString(externalId)}");

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
            // For single day, use itemCount=-1 to get all orders
            queryParams.Add("itemCount=-1");
        }
        else
        {
            // For multi-day, use pagination
            queryParams.Add($"page={page}");
            queryParams.Add($"pageSize={pageSize}");
        }

        var queryString = string.Join("&", queryParams);
        return $"{BaseUrl}?{queryString}";
    }

    private (List<GreifinnOrderDto> Orders, int TotalCount) ParseOrders(string html)
    {
        var orders = new List<GreifinnOrderDto>();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Extract total count from pagination info
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

        // Find order rows in the table body (rows with class altRow1 or altRow2)
        var orderRows = doc.DocumentNode.SelectNodes("//table//tbody//tr[contains(@class, 'altRow')]");

        if (orderRows != null)
        {
            foreach (var row in orderRows)
            {
                var order = ParseOrderRow(row);
                if (order != null)
                {
                    orders.Add(order);
                }
            }
        }
        else
        {
            _logger.LogWarning("Could not find order rows in HTML. Expected table rows with class 'altRow1' or 'altRow2'.");
        }

        // If we couldn't parse total count, use the number of orders found
        if (totalCount == 0)
        {
            totalCount = orders.Count;
        }

        _logger.LogInformation("Parsed {Count} orders from HTML (total: {TotalCount})", orders.Count, totalCount);
        return (orders, totalCount);
    }

    private GreifinnOrderDto? ParseOrderRow(HtmlNode row)
    {
        try
        {
            var cells = row.SelectNodes(".//td");
            if (cells == null || cells.Count < 11)
            {
                _logger.LogWarning("Order row has insufficient cells. Expected 11, found {Count}", cells?.Count ?? 0);
                return null;
            }

            var order = new GreifinnOrderDto();

            // Extract order ID from the link in the last column (toolCol)
            // Link format: /is/moya/pilot/order/print/{orderId}
            var link = row.SelectSingleNode(".//td[@class='toolCol last']//a[@href]");
            if (link != null)
            {
                var href = link.GetAttributeValue("href", "");
                if (!string.IsNullOrEmpty(href))
                {
                    order.DetailUrl = href.StartsWith("http") ? href : $"https://www.greifinn.is{href}";
                    
                    // Extract order ID from URL: /is/moya/pilot/order/print/267350
                    var orderIdMatch = System.Text.RegularExpressions.Regex.Match(href, @"/print/(\d+)$");
                    if (orderIdMatch.Success)
                    {
                        order.OrderId = orderIdMatch.Groups[1].Value;
                    }
                }
            }

            // Cell order (0-indexed):
            // 0: Phone number (class="first")
            // 1: Customer name
            // 2: Customer address
            // 3: Date (format: "dd.MM.yyyy HH:mm")
            // 4: Location name
            // 5: Delivery method name
            // 6: Payment method name
            // 7: Total price (format: "X.XXX kr." or "XXX kr.")
            // 8: External ID
            // 9: Status name
            // 10: Tool column (with link)

            order.PhoneNumber = cells[0].InnerText.Trim();
            order.CustomerName = cells[1].InnerText.Trim();
            order.CustomerAddress = cells[2].InnerText.Trim();
            
            // Parse date (Icelandic format: "dd.MM.yyyy HH:mm")
            var dateStr = cells[3].InnerText.Trim();
            if (DateTime.TryParseExact(dateStr, "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                order.AddedDate = date;
            }
            else if (DateTime.TryParseExact(dateStr, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnly))
            {
                order.AddedDate = dateOnly;
            }

            order.LocationName = cells[4].InnerText.Trim();
            order.DeliveryMethodName = cells[5].InnerText.Trim();
            order.PaymentMethodName = cells[6].InnerText.Trim();
            
            // Parse total price (format: "X.XXX kr." or "XXX kr.")
            var priceStr = cells[7].InnerText.Trim();
            order.TotalPrice = ParseIcelandicPrice(priceStr);

            order.ExternalId = cells[8].InnerText.Trim();
            order.StatusName = cells[9].InnerText.Trim();

            // If order ID wasn't found from link, try to generate a unique identifier
            if (string.IsNullOrEmpty(order.OrderId))
            {
                // Use external ID as fallback, or combination of date and phone
                order.OrderId = !string.IsNullOrEmpty(order.ExternalId) 
                    ? order.ExternalId 
                    : $"{order.AddedDate:yyyyMMddHHmm}_{order.PhoneNumber}";
            }

            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing order row. HTML: {RowHtml}", row.OuterHtml);
            return null;
        }
    }

    private decimal? ParseIcelandicPrice(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        try
        {
            // Remove currency symbols and whitespace
            // Format examples: "13.700 kr.", "410 kr.", "0 kr."
            value = value.Trim()
                .Replace("kr.", "", StringComparison.OrdinalIgnoreCase)
                .Replace("kr", "", StringComparison.OrdinalIgnoreCase)
                .Trim();
            
            // Icelandic format: 1.000 (dots = thousands separators)
            // Remove dots (thousands separators)
            value = value.Replace(".", "");
            
            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse Icelandic price: {Value}", value);
        }

        return null;
    }

    public async Task<GreifinnOrderDetailDto> GetOrderDetailAsync(string orderId, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"https://www.greifinn.is/is/moya/pilot/order/print/{orderId}";
            _logger.LogInformation("Scraping order detail from {Url}", url);

            // Get bytes and try to detect encoding
            var bytes = await _httpClient.GetByteArrayAsync(url, cancellationToken);
            
            // Try UTF-8 first (most common), then fall back to ISO-8859-1 if that fails
            string html;
            try
            {
                // Check if it's valid UTF-8 by trying to decode it
                var utf8Encoding = new UTF8Encoding(false, true); // throwOnInvalidBytes = true
                html = utf8Encoding.GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                // If UTF-8 fails, use ISO-8859-1 as specified in HTML meta tag
                _logger.LogDebug("UTF-8 decoding failed, falling back to ISO-8859-1");
                html = Encoding.GetEncoding("ISO-8859-1").GetString(bytes);
            }

            return ParseOrderDetail(html, orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping order detail for order ID {OrderId}", orderId);
            throw;
        }
    }

    private GreifinnOrderDetailDto ParseOrderDetail(string html, string orderId)
    {
        var doc = new HtmlDocument();
        // Set encoding option to handle ISO-8859-1 properly
        doc.OptionDefaultStreamEncoding = Encoding.GetEncoding("ISO-8859-1");
        doc.LoadHtml(html);

        var order = new GreifinnOrderDetailDto
        {
            OrderId = orderId,
            Items = new List<GreifinnOrderItemDto>()
        };

        // Parse info section
        var phoneNode = doc.DocumentNode.SelectSingleNode("//div[@class='phone']");
        if (phoneNode != null)
            order.PhoneNumber = DecodeText(phoneNode.InnerText.Trim());

        var customerNode = doc.DocumentNode.SelectSingleNode("//div[@class='customer']");
        if (customerNode != null)
            order.CustomerName = DecodeText(customerNode.InnerText.Trim());

        var addressNode = doc.DocumentNode.SelectSingleNode("//div[@class='address']");
        if (addressNode != null)
            order.CustomerAddress = DecodeText(addressNode.InnerText.Trim());

        var deliveryMethodNode = doc.DocumentNode.SelectSingleNode("//div[@class='deliveryMethod']");
        if (deliveryMethodNode != null)
            order.DeliveryMethod = DecodeText(deliveryMethodNode.InnerText.Trim());

        var paymentMethodNode = doc.DocumentNode.SelectSingleNode("//div[@class='paymentMethod']");
        if (paymentMethodNode != null)
            order.PaymentMethod = DecodeText(paymentMethodNode.InnerText.Trim());

        // Parse times section
        var readyTimeNode = doc.DocumentNode.SelectSingleNode("//div[@class='readyTime']");
        if (readyTimeNode != null)
        {
            var readyTimeText = readyTimeNode.InnerText.Trim();
            // Format: "Tilbúið kl: 17:17 -  6. júlí 2020"
            order.ReadyTime = ParseIcelandicDateTime(readyTimeText);
        }

        var addedTimeNode = doc.DocumentNode.SelectSingleNode("//div[@class='added']");
        if (addedTimeNode != null)
        {
            var addedTimeText = addedTimeNode.InnerText.Trim();
            // Format: "Pantað kl: 16:57 -  6. júlí 2020"
            order.AddedTime = ParseIcelandicDateTime(addedTimeText);
        }

        // Parse items table - the table is inside a div with class "items"
        var itemsTable = doc.DocumentNode.SelectSingleNode("//div[@class='items']//table");
        if (itemsTable != null)
        {
            var allRows = itemsTable.SelectNodes(".//tbody//tr");
            if (allRows != null)
            {
                _logger.LogInformation("Found {Count} total rows in items table", allRows.Count);
                GreifinnOrderItemDto? currentItem = null;
                
                foreach (var row in allRows)
                {
                    var rowClass = row.GetAttributeValue("class", "");
                    _logger.LogDebug("Processing row with class: {RowClass}", rowClass);
                    
                    // Check if it's an item row (has "item" class but not "option" or "quantity")
                    if (rowClass.Contains("item") && !rowClass.Contains("option") && !rowClass.Contains("quantity") && !rowClass.Contains("footer"))
                    {
                        // New item row - save previous item if exists
                        if (currentItem != null)
                        {
                            _logger.LogDebug("Saving previous item: {ItemName}", currentItem.Name);
                            order.Items.Add(currentItem);
                        }
                        
                        // Start new item
                        currentItem = ParseOrderItemHeader(row);
                        if (currentItem != null)
                        {
                            _logger.LogDebug("Started new item: {ItemName} (ID: {ItemId})", currentItem.Name, currentItem.ItemId);
                        }
                    }
                    else if (rowClass.Contains("option") && currentItem != null)
                    {
                        // Option row - add to current item
                        var option = ParseOrderOption(row);
                        if (option != null)
                        {
                            _logger.LogDebug("Added option to item: {OptionName}", option.Name);
                            currentItem.Options.Add(option);
                        }
                    }
                    else if (rowClass.Contains("quantity") && currentItem != null)
                    {
                        // Quantity row - finalize current item
                        _logger.LogDebug("Finalizing item with quantity row: {ItemName}", currentItem.Name);
                        ParseQuantityRow(row, currentItem);
                        order.Items.Add(currentItem);
                        currentItem = null;
                    }
                }
                
                // Add last item if exists
                if (currentItem != null)
                {
                    order.Items.Add(currentItem);
                }
            }
            else
            {
                _logger.LogWarning("No rows found in items table");
            }
        }
        else
        {
            _logger.LogWarning("Items table not found in order detail HTML");
        }

        // Parse total from footer
        var totalRow = doc.DocumentNode.SelectSingleNode("//tr[@class='footer separate']//td[@class='price']");
        if (totalRow != null)
        {
            var totalText = totalRow.InnerText.Trim();
            order.TotalPrice = ParseIcelandicPrice(totalText) ?? 0;
        }

        _logger.LogInformation("Parsed order detail for order ID {OrderId}: {ItemCount} items, Total: {Total}", 
            orderId, order.Items.Count, order.TotalPrice);

        return order;
    }

    private GreifinnOrderItemDto? ParseOrderItemHeader(HtmlNode itemRow)
    {
        try
        {
            var item = new GreifinnOrderItemDto
            {
                Options = new List<GreifinnOrderOptionDto>()
            };

            // Extract item ID from class (e.g., "item item-80" -> "80")
            var itemClass = itemRow.GetAttributeValue("class", "");
            var itemIdMatch = System.Text.RegularExpressions.Regex.Match(itemClass, @"item-(\d+)");
            if (itemIdMatch.Success)
            {
                item.ItemId = itemIdMatch.Groups[1].Value;
            }

            // Get item name from the row (first td with colspan="2")
            var nameCell = itemRow.SelectSingleNode(".//td[@colspan='2']") ?? itemRow.SelectSingleNode(".//td[1]");
            if (nameCell != null)
            {
                item.Name = DecodeText(nameCell.InnerText.Trim());
            }

            return item;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing order item header. HTML: {RowHtml}", itemRow.OuterHtml);
            return null;
        }
    }

    private GreifinnOrderOptionDto? ParseOrderOption(HtmlNode optionRow)
    {
        try
        {
            var option = new GreifinnOrderOptionDto();

            // Extract option ID and group ID from class (e.g., "option option-6 optionGroup-12")
            var optionClass = optionRow.GetAttributeValue("class", "");
            var optionIdMatch = System.Text.RegularExpressions.Regex.Match(optionClass, @"option-(\d+)");
            if (optionIdMatch.Success)
            {
                option.OptionId = optionIdMatch.Groups[1].Value;
            }

            var groupIdMatch = System.Text.RegularExpressions.Regex.Match(optionClass, @"optionGroup-(\d+)");
            if (groupIdMatch.Success)
            {
                option.OptionGroupId = groupIdMatch.Groups[1].Value;
            }

            // Get option name
            var nameCell = optionRow.SelectSingleNode(".//td[1]");
            if (nameCell != null)
            {
                option.Name = DecodeText(nameCell.InnerText.Trim());
            }

            // Get option price
            var priceCell = optionRow.SelectSingleNode(".//td[@class='price']");
            if (priceCell != null)
            {
                var priceText = priceCell.InnerText.Trim();
                option.Price = ParseIcelandicPrice(priceText) ?? 0;
            }

            return option;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing order option. HTML: {RowHtml}", optionRow.OuterHtml);
            return null;
        }
    }

    private void ParseQuantityRow(HtmlNode quantityRow, GreifinnOrderItemDto item)
    {
        try
        {
            // Format: "1 stk @ 1.520" -> quantity = 1, unitPrice = 1520
            var quantityCell = quantityRow.SelectSingleNode(".//td[1]");
            if (quantityCell != null)
            {
                var quantityText = quantityCell.InnerText.Trim();
                // Match pattern like "1 stk @ 1.520"
                var match = System.Text.RegularExpressions.Regex.Match(quantityText, @"(\d+)\s+stk\s+@\s+([\d.]+)");
                if (match.Success)
                {
                    if (int.TryParse(match.Groups[1].Value, out var qty))
                    {
                        item.Quantity = qty;
                    }
                    var unitPriceText = match.Groups[2].Value.Replace(".", "");
                    if (decimal.TryParse(unitPriceText, NumberStyles.Number, CultureInfo.InvariantCulture, out var unitPrice))
                    {
                        item.UnitPrice = unitPrice;
                    }
                }
            }

            // Get total price
            var priceCell = quantityRow.SelectSingleNode(".//td[@class='price']");
            if (priceCell != null)
            {
                var priceText = priceCell.InnerText.Trim();
                item.TotalPrice = ParseIcelandicPrice(priceText) ?? 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing quantity row. HTML: {RowHtml}", quantityRow.OuterHtml);
        }
    }

    private DateTime? ParseIcelandicDateTime(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        try
        {
            // Format: "Tilbúið kl: 17:17 -  6. júlí 2020" or "Pantað kl: 16:57 -  6. júlí 2020"
            // Extract time and date parts
            var timeMatch = System.Text.RegularExpressions.Regex.Match(text, @"kl:\s*(\d{1,2}):(\d{2})");
            var dateMatch = System.Text.RegularExpressions.Regex.Match(text, @"(\d{1,2})\.\s*(\w+)\s+(\d{4})");

            if (timeMatch.Success && dateMatch.Success)
            {
                var hour = int.Parse(timeMatch.Groups[1].Value);
                var minute = int.Parse(timeMatch.Groups[2].Value);
                var day = int.Parse(dateMatch.Groups[1].Value);
                var monthName = dateMatch.Groups[2].Value.ToLower();
                var year = int.Parse(dateMatch.Groups[3].Value);

                // Map Icelandic month names to numbers
                var monthMap = new Dictionary<string, int>
                {
                    { "janúar", 1 }, { "febrúar", 2 }, { "mars", 3 }, { "apríl", 4 },
                    { "maí", 5 }, { "júní", 6 }, { "júlí", 7 }, { "ágúst", 8 },
                    { "september", 9 }, { "október", 10 }, { "nóvember", 11 }, { "desember", 12 }
                };

                if (monthMap.TryGetValue(monthName, out var month))
                {
                    return new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse Icelandic datetime: {Text}", text);
        }

        return null;
    }

    /// <summary>
    /// Ensures text is properly decoded. Decodes HTML entities and fixes any encoding issues.
    /// </summary>
    private string DecodeText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Decode HTML entities (e.g., &nbsp;, &amp;, etc.)
        text = System.Net.WebUtility.HtmlDecode(text);
        
        // Fix common encoding issues where UTF-8 bytes were interpreted as ISO-8859-1
        // This happens when text like "Sótt" (UTF-8: SÃ³tt) is incorrectly decoded
        try
        {
            // If the text contains mojibake (garbled characters), try to fix it
            // by re-encoding as ISO-8859-1 bytes and decoding as UTF-8
            var isoBytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(text);
            var fixedText = Encoding.UTF8.GetString(isoBytes);
            
            // Only use fixed text if it contains valid UTF-8 characters (no replacement chars)
            if (!fixedText.Contains('\uFFFD')) // U+FFFD is the replacement character
            {
                return fixedText;
            }
        }
        catch
        {
            // If conversion fails, return original
        }
        
        return text;
    }
}

