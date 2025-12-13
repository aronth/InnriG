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
                    var lineNumStr = Regex.Replace(cells[0].InnerText, @"[^\d]", "");
                    if (!int.TryParse(lineNumStr, out var lineNum)) continue;

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
                    var lineNumStr = Regex.Replace(cells[0].InnerText, @"[^\d]", "");
                    int.TryParse(lineNumStr, out var currentLineNum);

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
                    
                    if (extraInfo.ContainsKey(currentLineNum))
                    {
                        listPrice = extraInfo[currentLineNum].ListPrice;
                        discount = extraInfo[currentLineNum].Discount;
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
                        Discount = discount,
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
        var match = Regex.Match(input, @"[\d.,]+");
        if (!match.Success) return 0;

        var valStr = match.Value;
        // Icelandic: 1.000,00 -> remove dots, repl comma with dot
        var clean = valStr.Replace(".", "").Replace(",", ".");
        if (decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }
        return 0;
    }
}
