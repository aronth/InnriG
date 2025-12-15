using HtmlAgilityPack;
using InnriGreifi.API.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace InnriGreifi.API.Services.Parsers;

public class TableInvoiceSubParser : IInvoiceSubParser
{
    public bool CanParse(string supplierName, HtmlDocument doc)
    {
        // Heuristic: Look for a table with specific headers
        var tables = doc.DocumentNode.SelectNodes("//table");
        if (tables == null) return false;

        foreach (var table in tables)
        {
            var text = table.InnerText;
            if (text.Contains("Vörunr") && text.Contains("Lýsing") && text.Contains("Magn"))
            {
                return true;
            }
        }
        return false;
    }

    public List<InvoiceItem> ParseItems(HtmlDocument doc, Guid invoiceId)
    {
        var items = new List<InvoiceItem>();
        var tables = doc.DocumentNode.SelectNodes("//table");
        if (tables == null) return items;

        HtmlNode itemsTable = null;
        HtmlNode discountsTable = null;
        var detailedLineInfo = ExtractDetailedLineInfo(doc);
        var detailedByItemId = ExtractDetailedLineInfoByItemId(doc);

        // Identify tables
        foreach (var table in tables)
        {
            var text = table.InnerText;
            if (text.Contains("Vörunr") && text.Contains("Lýsing") && text.Contains("Magn") && itemsTable == null)
            {
                itemsTable = table;
            }
            if (text.Contains("Línunr.") && text.Contains("Listaverð"))
            {
                discountsTable = table;
            }
        }

        // Dictionary to hold extra info (ListPrice) keyed by Line Number (int)
        var extraInfo = new Dictionary<int, (decimal ListPrice, decimal Discount)>();

        // 1. Parse Discounts/List Price Table first if available
        if (discountsTable != null)
        {
            var rows = discountsTable.SelectNodes(".//tr");
            if (rows != null)
            {
                foreach (var row in rows)
                {
                    var cells = row.SelectNodes(".//td");
                    if (cells == null || cells.Count < 6) continue;

                    // Header check skip
                    if (row.ParentNode.Name == "thead") continue;

                    // Col 0: "2." -> Line Number
                    // Robust extraction: Remove all non-digits
                    var lineNum = ExtractLineNumber(cells[0].InnerText);
                    if (lineNum == 0) continue;

                    // Col 3: Listaverð "309,00"
                    var listPrice = ParseDecimal(cells[3].InnerText);
                    var discount = ParseDecimal(cells[4].InnerText);
                    
                    if (!extraInfo.ContainsKey(lineNum))
                    {
                        extraInfo[lineNum] = (listPrice, discount);
                    }
                }
            }
        }

        // 2. Parse Items Table
        if (itemsTable != null)
        {
            var rows = itemsTable.SelectNodes(".//tr");
            if (rows != null)
            {
                foreach (var row in rows)
                {
                    var cells = row.SelectNodes(".//td");
                    if (cells == null || cells.Count < 8) continue;

                    // Check for header
                    if (row.ParentNode.Name == "thead") continue;
                    
                    // Check if spacer row
                    if (string.IsNullOrWhiteSpace(cells[1].InnerText)) continue;

                    // Extract Line Number from Col 0 "1."
                    var currentLineNum = ExtractLineNumber(cells[0].InnerText);

                    var itemId = cells[1].InnerText.Trim();
                    if (string.IsNullOrWhiteSpace(itemId) || itemId.StartsWith("Samtals", StringComparison.OrdinalIgnoreCase)) continue;

                    var rawName = cells[2].InnerText;
                    var name = CleanName(rawName);

                    var qty = ParseDecimal(cells[3].InnerText);
                    var unit = cells[4].InnerText.Trim();
                    var unitPrice = ParseDecimal(cells[5].InnerText); // This is Net Price usually
                    var vatCode = cells[6].InnerText.Trim(); // VSK
                    // Skip Discount col 7, Total col 8

                    var total = ParseDecimal(cells[8].InnerText);
                    // Col 9 is TotalWithVat if exists
                    decimal totalWithVat = 0;
                    if (cells.Count > 9)
                    {
                        totalWithVat = ParseDecimal(cells[9].InnerText);
                    }

                    // Resolve List Price
                    decimal listPrice = unitPrice; // Default to unit price if no detailed info
                    decimal discount = 0;
                    decimal discountPercent = 0;

                    if (detailedByItemId.TryGetValue(itemId, out var detailById))
                    {
                        if (detailById.SalesPrice.HasValue && detailById.SalesPrice.Value > 0)
                        {
                            listPrice = detailById.SalesPrice.Value;
                        }

                        if (detailById.LineAmount.HasValue && detailById.LineAmount.Value > 0)
                        {
                            var qtyForCalc = qty != 0 ? qty : 1;
                            var discountedUnitPrice = detailById.LineAmount.Value / qtyForCalc;
                            unitPrice = discountedUnitPrice;
                            total = detailById.LineAmount.Value;

                            if (discount == 0 && listPrice > 0 && discountedUnitPrice > 0)
                            {
                                var calculatedDiscount = listPrice - discountedUnitPrice;
                                if (calculatedDiscount > 0)
                                {
                                    discount = calculatedDiscount;
                                    discountPercent = Math.Round((discount / listPrice) * 100, 2);
                                }
                            }
                        }
                    }
                    else if (extraInfo.ContainsKey(currentLineNum))
                    {
                        listPrice = extraInfo[currentLineNum].ListPrice;
                        discount = extraInfo[currentLineNum].Discount;
                        if (listPrice > 0 && discount > 0)
                        {
                            discountPercent = Math.Round((discount / listPrice) * 100, 2);
                        }
                    }

                    if (detailedLineInfo.TryGetValue(currentLineNum, out var detailed))
                    {
                        if (detailed.SalesPrice.HasValue && detailed.SalesPrice.Value > 0)
                        {
                            listPrice = detailed.SalesPrice.Value;
                        }

                        if (detailed.LineAmount.HasValue && detailed.LineAmount.Value > 0)
                        {
                            var qtyForCalc = qty != 0 ? qty : 1;
                            var discountedUnitPrice = detailed.LineAmount.Value / qtyForCalc;
                            unitPrice = discountedUnitPrice;
                            total = detailed.LineAmount.Value;

                            if (discount == 0 && listPrice > 0 && discountedUnitPrice > 0)
                            {
                                var calculatedDiscount = listPrice - discountedUnitPrice;
                                if (calculatedDiscount > 0)
                                {
                                    discount = calculatedDiscount;
                                    discountPercent = Math.Round((discount / listPrice) * 100, 2);
                                }
                            }
                        }
                    }

                    var item = new InvoiceItem
                    {
                        Id = Guid.NewGuid(),
                        InvoiceId = invoiceId,
                        ItemId = itemId,
                        ItemName = name,
                        Quantity = qty,
                        Unit = unit,
                        UnitPrice = unitPrice,
                        ListPrice = listPrice,
                        Discount = discountPercent, // store percentage instead of amount
                        VatCode = vatCode,
                        TotalPrice = total,
                        TotalPriceWithVat = totalWithVat
                    };

                    // Basic validation
                    if (item.Quantity != 0 || item.TotalPrice != 0)
                    {
                        items.Add(item);
                    }
                }
            }
        }

        return items;
    }

    private Dictionary<int, (decimal? SalesPrice, decimal? LineAmount)> ExtractDetailedLineInfo(HtmlDocument doc)
    {
        var map = new Dictionary<int, (decimal? SalesPrice, decimal? LineAmount)>();

        // Find the detail table that contains the OES* values
        HtmlNode? detailTable = doc.DocumentNode
            .SelectSingleNode("//div[contains(normalize-space(text()), 'Ítarupplýsingar á línum')]/following::table[1]");

        // Fallback: any table containing OESSalesPrice
        detailTable ??= doc.DocumentNode.SelectSingleNode("//table[.//text()[contains(., 'OESSalesPrice')]]");
        if (detailTable == null) return map;

        var rows = detailTable.SelectNodes(".//tr");
        if (rows == null) return map;

        var currentLine = -1;
        foreach (var row in rows)
        {
            var cells = row.SelectNodes("./td");
            // Only set line number on top-level detail rows (avoid nested tables where amounts appear)
            if (cells != null && cells.Count > 1 && IsTopLevelDetailRow(row, detailTable))
            {
                var maybeLine = ExtractLineNumber(cells[1].InnerText);
                if (maybeLine > 0)
                {
                    currentLine = maybeLine;
                }
            }

            var nestedDetailTable = row.SelectSingleNode(".//table[.//text()[contains(., 'OESSalesPrice')]]");
            if (nestedDetailTable != null && currentLine > 0)
            {
                decimal? salesPrice = null;
                decimal? lineAmount = null;

                var salesPriceNode = nestedDetailTable.SelectSingleNode(".//td[contains(., 'OESSalesPrice')]");
                if (salesPriceNode != null)
                {
                    salesPrice = ParseDecimal(salesPriceNode.InnerText);
                }

                var lineAmountNode = nestedDetailTable.SelectSingleNode(".//td[contains(., 'OESLineAmount')]");
                if (lineAmountNode != null)
                {
                    lineAmount = ParseDecimal(lineAmountNode.InnerText);
                }

                map[currentLine] = (salesPrice, lineAmount);
            }
        }

        return map;
    }

    private Dictionary<string, (decimal? SalesPrice, decimal? LineAmount)> ExtractDetailedLineInfoByItemId(HtmlDocument doc)
    {
        var map = new Dictionary<string, (decimal? SalesPrice, decimal? LineAmount)>(StringComparer.OrdinalIgnoreCase);

        HtmlNode? detailTable = doc.DocumentNode
            .SelectSingleNode("//div[contains(normalize-space(text()), 'Ítarupplýsingar á línum')]/following::table[1]");
        detailTable ??= doc.DocumentNode.SelectSingleNode("//table[.//text()[contains(., 'OESSalesPrice')]]");
        if (detailTable == null) return map;

        var rows = detailTable.SelectNodes(".//tr");
        if (rows == null || rows.Count < 3) return map;

        for (int i = 0; i < rows.Count - 2; i++)
        {
            var firstRowCells = rows[i].SelectNodes("./td");
            if (firstRowCells == null || firstRowCells.Count < 3) continue;
            var itemId = firstRowCells[2].InnerText.Trim();
            if (string.IsNullOrWhiteSpace(itemId)) continue;

            var thirdRow = rows[i + 2];
            var nestedDetailTable = thirdRow.SelectSingleNode(".//table[.//text()[contains(., 'OESSalesPrice')]]");
            if (nestedDetailTable == null) continue;

            decimal? salesPrice = null;
            decimal? lineAmount = null;

            var salesPriceNode = nestedDetailTable.SelectSingleNode(".//td[contains(., 'OESSalesPrice')]");
            if (salesPriceNode != null)
            {
                salesPrice = ParseDecimal(salesPriceNode.InnerText);
            }

            var lineAmountNode = nestedDetailTable.SelectSingleNode(".//td[contains(., 'OESLineAmount')]");
            if (lineAmountNode != null)
            {
                lineAmount = ParseDecimal(lineAmountNode.InnerText);
            }

            map[itemId] = (salesPrice, lineAmount);

            i += 2; // skip the name row and detail table row we just consumed
        }

        return map;
    }

    private bool IsTopLevelDetailRow(HtmlNode row, HtmlNode detailTable)
    {
        // Top-level rows are direct children of the detail table (or its tbody)
        return row.ParentNode == detailTable ||
               (row.ParentNode?.ParentNode != null && row.ParentNode.ParentNode == detailTable);
    }

    private int ExtractLineNumber(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return 0;

        // Some invoices use "1.0000000000." — take the first integer chunk.
        var match = Regex.Match(input, @"\d+");
        if (!match.Success) return 0;

        return int.TryParse(match.Value, out var result) ? result : 0;
    }

    private string CleanName(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        // Basic cleanup first
        var cleaned = input.Replace("|", " ").Replace("\n", " ").Replace("\r", " ");
        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
        // Apply advanced cleaning
        return HtmlInvoiceParser.CleanItemName(cleaned);
    }

    private decimal ParseDecimal(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return 0;

        // Detect decimal separator: if there's a comma, we assume Icelandic (comma decimal, dot thousands).
        // If there's no comma but there is a dot, we treat dot as decimal separator (en-US style).
        var hasComma = input.Contains(',');
        var hasDot = input.Contains('.');

        var match = Regex.Match(input, @"[\d\.,]+");
        if (!match.Success) return 0;

        var valStr = match.Value;

        string clean;
        if (hasComma)
        {
            // Icelandic style: strip thousand dots, turn comma into dot
            clean = valStr.Replace(".", "").Replace(",", ".");
        }
        else
        {
            // Dot decimal (or integer with dots already stripped by regex)
            clean = valStr;
        }

        return decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result
            : 0;
    }
}
