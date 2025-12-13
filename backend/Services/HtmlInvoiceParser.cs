using HtmlAgilityPack;
using InnriGreifi.API.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace InnriGreifi.API.Services;

using InnriGreifi.API.Services.Parsers;


public class HtmlInvoiceParser : IInvoiceParser
{
    private readonly IEnumerable<IInvoiceSubParser> _subParsers;

    public HtmlInvoiceParser()
    {
        _subParsers = new List<IInvoiceSubParser>
        {
            new TableInvoiceSubParser(),
            new DivInvoiceSubParser()
        };
    }

    public Invoice ParseInvoice(string htmlContent, string fileName)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(htmlContent);

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            InvoiceNumber = Path.GetFileNameWithoutExtension(fileName) // Fallback
        };

        var allText = doc.DocumentNode.InnerText;
        var lines = allText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(l => l.Trim())
                           .Where(l => !string.IsNullOrWhiteSpace(l))
                           .ToList();

        // 1. Extract Supplier
        // Try structured first
        var supplierDiv = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'dalkfyrirsogn2')]");
        if (supplierDiv != null)
        {
            invoice.SupplierName = CleanSupplierName(supplierDiv.InnerText.Trim());
        }
        else
        {
            // Fallback: Look for seller_info with <b> tag for name only
            var sellerNode = doc.DocumentNode.SelectSingleNode("//*[contains(@class, 'seller_info')]");
            if (sellerNode != null)
            {
                // Try to get just the <b> tag which contains the company name
                var boldNode = sellerNode.SelectSingleNode(".//b");
                if (boldNode != null)
                {
                    invoice.SupplierName = CleanSupplierName(boldNode.InnerText.Trim());
                }
                else
                {
                    // Fallback to old logic
                    var nameNode = sellerNode.SelectSingleNode(".//h2") ?? sellerNode.SelectSingleNode(".//div[contains(@class, 'name')]");
                    if (nameNode != null) invoice.SupplierName = CleanSupplierName(nameNode.InnerText.Trim());
                }
            }

            // Text scan fallback from Python
            if (string.IsNullOrEmpty(invoice.SupplierName))
            {
               int seljandiIdx = lines.FindIndex(l => l.Contains("Seljandi"));
               if (seljandiIdx != -1)
               {
                   for (int i = seljandiIdx + 1; i < Math.Min(seljandiIdx + 10, lines.Count); i++)
                   {
                        var line = lines[i];
                        if (line.Contains("ehf", StringComparison.OrdinalIgnoreCase) || 
                            line.Contains("hf", StringComparison.OrdinalIgnoreCase) || 
                            line.Contains("slf", StringComparison.OrdinalIgnoreCase))
                        {
                             // Simple extraction
                             invoice.SupplierName = CleanSupplierName(line);
                             break;
                        }
                   }
               }
            }
        }
        if (string.IsNullOrEmpty(invoice.SupplierName)) invoice.SupplierName = "Unknown Supplier";

        // 1.5. Extract Buyer (Kaupandi)
        // Try new format first: buyer_info class
        var buyerInfoNode = doc.DocumentNode.SelectSingleNode("//*[contains(@class, 'buyer_info')]");
        if (buyerInfoNode != null)
        {
            var buyerBoldNode = buyerInfoNode.SelectSingleNode(".//b");
            if (buyerBoldNode != null)
            {
                invoice.BuyerName = CleanBuyerName(buyerBoldNode.InnerText.Trim());
            }
            else
            {
                // Fallback: get first line
                var firstLine = buyerInfoNode.InnerText.Split(new[] { "\r\n", "\r", "\n", "<br>" }, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault(l => !string.IsNullOrWhiteSpace(l.Trim()));
                if (firstLine != null)
                {
                    invoice.BuyerName = CleanBuyerName(firstLine.Trim());
                }
            }
        }
        else
        {
            // Try old format: "Kaupandi:" label followed by content
            var kaupandiLabel = doc.DocumentNode.SelectNodes("//*[text()[contains(., 'Kaupandi:') or contains(., 'Kaupandi')]]");
            if (kaupandiLabel != null)
            {
                foreach (var labelNode in kaupandiLabel)
                {
                    // Look for <b> tag in parent or following siblings
                    var parent = labelNode.ParentNode;
                    if (parent != null)
                    {
                        var boldNode = parent.SelectSingleNode(".//b");
                        if (boldNode != null)
                        {
                            invoice.BuyerName = CleanBuyerName(boldNode.InnerText.Trim());
                            break;
                        }
                        
                        // Or look in following ListItem divs
                        var listItems = parent.SelectNodes(".//div[contains(@class, 'ListItem')]");
                        if (listItems != null && listItems.Count > 0)
                        {
                            var firstBold = listItems[0].SelectSingleNode(".//b");
                            if (firstBold != null)
                            {
                                invoice.BuyerName = CleanBuyerName(firstBold.InnerText.Trim());
                                break;
                            }
                            else if (!string.IsNullOrWhiteSpace(listItems[0].InnerText))
                            {
                                invoice.BuyerName = CleanBuyerName(listItems[0].InnerText.Trim());
                                break;
                            }
                        }
                    }
                }
            }
            
            // Text scan fallback
            if (string.IsNullOrEmpty(invoice.BuyerName))
            {
                int kaupandiIdx = lines.FindIndex(l => l.Contains("Kaupandi"));
                if (kaupandiIdx != -1)
                {
                    for (int i = kaupandiIdx + 1; i < Math.Min(kaupandiIdx + 10, lines.Count); i++)
                    {
                        var line = lines[i];
                        if (line.Contains("ehf", StringComparison.OrdinalIgnoreCase) || 
                            line.Contains("hf", StringComparison.OrdinalIgnoreCase) || 
                            line.Contains("slf", StringComparison.OrdinalIgnoreCase) ||
                            !string.IsNullOrWhiteSpace(line))
                        {
                            invoice.BuyerName = CleanBuyerName(line);
                            break;
                        }
                    }
                }
            }
        }

        // 2. Extract Date
        // Specific scan for "Útgáfudagur reiknings" in P tags or Divs directly
        var dateNodes = doc.DocumentNode.SelectNodes("//*[text()[contains(., 'Útgáfudagur reiknings') or contains(., 'Utgafudagur reiknings')]]");
        if (dateNodes != null)
        {
             var dateRegex = new Regex(@"(\d{2}\.\d{2}\.\d{4})");
             foreach(var node in dateNodes)
             {
                 // The text might be in the node or its parent/children (e.g. <p><b>Label</b><br>Date</p>)
                 // Check the parent's full text if the node is just the label
                 var containerText = node.ParentNode?.InnerText ?? node.InnerText;
                 var match = dateRegex.Match(containerText);
                 if (match.Success && DateTime.TryParseExact(match.Value, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                 {
                     invoice.InvoiceDate = date;
                     break;
                 }
             }
        }
        
        // Fallback Date
        if (invoice.InvoiceDate == default)
        {
             var dateRegex = new Regex(@"(\d{2}\.\d{2}\.\d{4})");
             // Strict scan of lines
             foreach(var line in lines)
             {
                 if (line.IndexOf("Útgáfudagur reiknings", StringComparison.OrdinalIgnoreCase) >= 0)
                 {
                      var match = dateRegex.Match(line);
                      if (match.Success && DateTime.TryParseExact(match.Value, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date))
                         invoice.InvoiceDate = date;
                 }
             }
        }


        // 3. Extract Totals
        // .payable_amount contains the total
        var totalNode = doc.DocumentNode.SelectSingleNode("//*[contains(@class, 'payable_amount')]");
        if (totalNode != null)
        {
             invoice.TotalAmount = ParseDecimal(totalNode.InnerText);
        }

        // 4. Parse Items using Sub-Parsers
        foreach (var parser in _subParsers)
        {
            if (parser.CanParse(invoice.SupplierName, doc))
            {
                var items = parser.ParseItems(doc, invoice.Id);
                if (items.Any())
                {
                    invoice.Items = items;
                    break;
                }
            }
        }


        return invoice;
    }

    private bool IsDecimal(string input)
    {
         var clean = input.Trim().Replace(".", "").Replace(",", ".");
         return decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out _);
    }

    private decimal ParseDecimal(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return 0;
        // Clean input
        // Icelandic: 1.000,00 -> remove dots, repl comma with dot
        // Be careful with "10,00 KGM" -> regex extraction needed?
        
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

    private string CleanSupplierName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName)) return rawName;

        // Remove address patterns (comma followed by address-like content)
        // Example: "Company Name ehf., Somestreet 123, 101 Reykjavik" -> "Company Name ehf."
        var addressPatterns = new[]
        {
            @",\s*[A-ZÞÆÖÁÍÉÝÚÓa-zþæöáíéýúó\s]+\s+\d+",  // ", Street 123"
            @",\s*\d{3}\s+[A-ZÞÆÖÁÍÉÝÚÓa-zþæöáíéýúó]",   // ", 101 Reykjavik"
        };

        var cleaned = rawName;
        foreach (var pattern in addressPatterns)
        {
            cleaned = Regex.Replace(cleaned, pattern + ".*$", "", RegexOptions.IgnoreCase);
        }

        // Trim and return
        return cleaned.Trim().TrimEnd(',', '.');
    }

    private string CleanBuyerName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName)) return rawName;

        // Similar to CleanSupplierName but for buyer names
        // Remove address patterns
        var addressPatterns = new[]
        {
            @",\s*[A-ZÞÆÖÁÍÉÝÚÓa-zþæöáíéýúó\s]+\s+\d+",  // ", Street 123"
            @",\s*\d{3}\s+[A-ZÞÆÖÁÍÉÝÚÓa-zþæöáíéýúó]",   // ", 101 Reykjavik"
            @",\s*IS\s*$",                                 // ", IS" at end
        };

        var cleaned = rawName;
        foreach (var pattern in addressPatterns)
        {
            cleaned = Regex.Replace(cleaned, pattern + ".*$", "", RegexOptions.IgnoreCase);
        }

        // Remove tax ID patterns (Icelandic kennitala: 10 digits)
        cleaned = Regex.Replace(cleaned, @"\b\d{6}[-]?\d{4}\b", "");

        // Trim and return
        return cleaned.Trim().TrimEnd(',', '.');
    }

    public static string CleanItemName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName)) return rawName;

        var cleaned = rawName;

        // Remove stock/warehouse numbers
        // Pattern: "ItemName LagerNr: 12345" or "ItemName (Lager: 12345)"
        cleaned = Regex.Replace(cleaned, @"\s*\(?Lager(Nr)?:?\s*\d+\)?", "", RegexOptions.IgnoreCase);
        
        // Remove category/group info
        // Pattern: "ItemName [Category]" or "ItemName (Category)"
        cleaned = Regex.Replace(cleaned, @"\s*[\[\(][^\]\)]+[\]\)]", "");
        
        // Remove extra metadata like "Vörunr: 123"
        cleaned = Regex.Replace(cleaned, @"\s*Vörunr:?\s*\d+", "", RegexOptions.IgnoreCase);
        
        // Remove "EAN:" codes
        cleaned = Regex.Replace(cleaned, @"\s*EAN:?\s*[\d\-]+", "", RegexOptions.IgnoreCase);
        
        // Remove trailing/leading whitespace and punctuation
        cleaned = cleaned.Trim().TrimEnd(',', '.', '-');
        
        // Remove multiple spaces
        cleaned = Regex.Replace(cleaned, @"\s{2,}", " ");

        return cleaned;
    }
}
