using HtmlAgilityPack;
using InnriGreifi.API.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace InnriGreifi.API.Services.Parsers;

public class DivInvoiceSubParser : IInvoiceSubParser
{
    public bool CanParse(string supplierName, HtmlDocument doc)
    {
        // Heuristic: Look for specific class names typical of Sendill/Peppol div layouts
        return doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'items_table_body_holder')]") != null;
    }

    public List<InvoiceItem> ParseItems(HtmlDocument doc, Guid invoiceId)
    {
        var items = new List<InvoiceItem>();
        var itemHolders = doc.DocumentNode.SelectNodes("//div[contains(@class, 'items_table_body_holder')]");
        if (itemHolders == null) return items;

        foreach (var holder in itemHolders)
        {
            // Exclude inner divs like 'items_table_body_data_name_column_header' which also contain the class string
            // We want the direct children that are 'items_table_body_data'
            var dataDivs = holder.SelectNodes(".//div[contains(@class, 'items_table_body_data') and not(contains(@class, 'column_header'))]");
            if (dataDivs == null || dataDivs.Count < 5) continue;

            var texts = dataDivs.Select(d => d.InnerText.Trim()).ToList();

            // Expected Index Mapping:
            // 0: Line Num "1."
            // 1: Item ID "6311"
            // 2: Name "Rækja..."
            // 3: Qty + Unit "10,00 KGM"
            // 4: Unit Price "1.990,00"
            // 5: VSK "S11"
            // 6: Total Net "19.900,00"
            // 7: Total Gross "22.089,00"

            if (texts.Count < 4) continue;

            var itemId = texts[1];
            // ID validation
            if (!Regex.IsMatch(itemId, @"^[\w\-\.]+$", RegexOptions.IgnoreCase))
            {
                // Fallback or skip? Sometimes ID is empty?
                // Peppol IDs are usually strict. 
            }

            var rawName = texts[2];
            
            // Better approach: Find the specific name header div within this holder
            // to avoid getting the <small> tag content
            var nameHeaderDiv = holder.SelectSingleNode(".//div[contains(@class, 'items_table_body_data_name_column_header')]");
            if (nameHeaderDiv != null)
            {
                // Get only direct text content, not nested elements
                var nameText = nameHeaderDiv.ChildNodes
                    .Where(n => n.NodeType == HtmlNodeType.Text)
                    .Select(n => n.InnerText.Trim())
                    .FirstOrDefault() ?? rawName;
                rawName = nameText;
            }
            
            var name = CleanName(rawName);

            // Parse Qty "10,00 KGM"
            decimal qty = 0;
            string unit = "";
            var qtyMatch = Regex.Match(texts[3], @"([\d.,]+)\s*([A-Za-z]*)");
            if (qtyMatch.Success)
            {
                qty = ParseDecimal(qtyMatch.Groups[1].Value);
                unit = qtyMatch.Groups[2].Value;
            }
            else
            {
                qty = ParseDecimal(texts[3]);
            }

            decimal unitPrice = 0;
            if (texts.Count > 4) unitPrice = ParseDecimal(texts[4]);

            string vatCode = "";
            if (texts.Count > 5) vatCode = texts[5].Trim();

            decimal total = 0;
            if (texts.Count > 6) total = ParseDecimal(texts[6]);

            decimal totalWithVat = 0;
            if (texts.Count > 7) totalWithVat = ParseDecimal(texts[7]);

            // Deduction: ListPrice = UnitPrice (no separate column in this layout usually)
            // If discount exists, it's not clearly separated in the main columns in this layout
            decimal listPrice = unitPrice; 
            decimal discount = 0;

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

            items.Add(item);
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
        var clean = valStr.Replace(".", "").Replace(",", ".");
        if (decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }
        return 0;
    }
}
