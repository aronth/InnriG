using HtmlAgilityPack;
using InnriGreifi.API.Models;
using System.Globalization;
using System.Linq;
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

        // Extract Invoice Number from HTML
        // Strategy 1: Look for "REIKNINGUR" followed by number in h1 tag
        var reikningurH1 = doc.DocumentNode.SelectSingleNode("//h1[contains(text(), 'REIKNINGUR') or contains(text(), 'Reikningur')]");
        if (reikningurH1 != null)
        {
            var h1Text = reikningurH1.InnerText;
            // Extract number after "REIKNINGUR" - could be space, dash, or directly after
            var invoiceNumberMatch = Regex.Match(h1Text, @"(?:REIKNINGUR|Reikningur)[\s\-:]+([A-Za-z0-9\-]+)", RegexOptions.IgnoreCase);
            if (invoiceNumberMatch.Success)
            {
                invoice.InvoiceNumber = invoiceNumberMatch.Groups[1].Value.Trim();
            }
            else
            {
                // Fallback: extract any alphanumeric sequence after REIKNINGUR
                var numberMatch = Regex.Match(h1Text, @"(?:REIKNINGUR|Reikningur)[\s]*([A-Za-z0-9\-]+)", RegexOptions.IgnoreCase);
                if (numberMatch.Success)
                {
                    invoice.InvoiceNumber = numberMatch.Groups[1].Value.Trim();
                }
            }
        }
        
        // Strategy 2: Look for container with "REIKNINGUR" and then find "Nr." in same container or siblings
        if (string.IsNullOrEmpty(invoice.InvoiceNumber) || invoice.InvoiceNumber == Path.GetFileNameWithoutExtension(fileName))
        {
            var reikningurContainer = doc.DocumentNode.SelectSingleNode("//*[contains(@class, 'righthausreikningur') or contains(@id, 'hausreikningur')]");
            if (reikningurContainer != null)
            {
                // Get the full text of the container (handles &nbsp; and line breaks)
                var containerText = System.Net.WebUtility.HtmlDecode(reikningurContainer.InnerText);
                // Look for "Nr." pattern in the container - handle whitespace, &nbsp;, and line breaks
                var nrMatch = Regex.Match(containerText, @"Nr\.?\s*:?\s*([A-Za-z0-9\-]+)", RegexOptions.IgnoreCase);
                if (nrMatch.Success)
                {
                    invoice.InvoiceNumber = nrMatch.Groups[1].Value.Trim();
                }
                else
                {
                    // If "REIKNINGUR" is in container, look for number in sibling divs
                    var reikningurDiv = reikningurContainer.SelectSingleNode(".//*[contains(text(), 'REIKNINGUR') or contains(text(), 'Reikningur')]");
                    if (reikningurDiv != null)
                    {
                        var parent = reikningurDiv.ParentNode;
                        if (parent != null)
                        {
                            // Look for divs with numbers after REIKNINGUR div
                            var siblingDivs = parent.SelectNodes(".//div");
                            if (siblingDivs != null)
                            {
                                bool foundReikningur = false;
                                foreach (var div in siblingDivs)
                                {
                                    if (foundReikningur)
                                    {
                                        var divText = System.Net.WebUtility.HtmlDecode(div.InnerText).Trim();
                                        // Check if this div contains "Nr." and a number
                                        var nrInDiv = Regex.Match(divText, @"Nr\.?\s*:?\s*([A-Za-z0-9\-]+)", RegexOptions.IgnoreCase);
                                        if (nrInDiv.Success)
                                        {
                                            invoice.InvoiceNumber = nrInDiv.Groups[1].Value.Trim();
                                            break;
                                        }
                                        // Or just extract a number if it looks like an invoice number (4+ digits or alphanumeric)
                                        var numberOnly = Regex.Match(divText, @"^([A-Za-z0-9\-]+)$");
                                        if (numberOnly.Success && (divText.Length >= 4 || Regex.IsMatch(divText, @"^\d{4,}")))
                                        {
                                            invoice.InvoiceNumber = numberOnly.Groups[1].Value.Trim();
                                            break;
                                        }
                                    }
                                    if (div.InnerText.Contains("REIKNINGUR", StringComparison.OrdinalIgnoreCase))
                                    {
                                        foundReikningur = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        // Strategy 3: Look for "Nr." or "Nr:" followed by number (general search)
        if (string.IsNullOrEmpty(invoice.InvoiceNumber) || invoice.InvoiceNumber == Path.GetFileNameWithoutExtension(fileName))
        {
            var nrNodes = doc.DocumentNode.SelectNodes("//*[text()[contains(., 'Nr.') or contains(., 'Nr:') or contains(., 'Nr ')]]");
            if (nrNodes != null)
            {
                foreach (var node in nrNodes)
                {
                    // Get parent or container text to catch cases where "Nr." and number are in different nodes
                    var container = node.ParentNode ?? node;
                    // Decode HTML entities like &nbsp;
                    var text = System.Net.WebUtility.HtmlDecode(container.InnerText);
                    // Pattern: "Nr." or "Nr:" followed by whitespace and then the number
                    var nrMatch = Regex.Match(text, @"Nr\.?\s*:?\s*([A-Za-z0-9\-]+)", RegexOptions.IgnoreCase);
                    if (nrMatch.Success)
                    {
                        var extractedNumber = nrMatch.Groups[1].Value.Trim();
                        // Make sure it's not just a small number (like line numbers) - invoice numbers are usually 4+ digits
                        if (extractedNumber.Length >= 4 || Regex.IsMatch(extractedNumber, @"^\d{4,}"))
                        {
                            invoice.InvoiceNumber = extractedNumber;
                            break;
                        }
                    }
                }
            }
        }
        
        // Strategy 4: Look for document_details class with invoice number
        if (string.IsNullOrEmpty(invoice.InvoiceNumber) || invoice.InvoiceNumber == Path.GetFileNameWithoutExtension(fileName))
        {
            var docDetailsNode = doc.DocumentNode.SelectSingleNode("//*[contains(@class, 'document_details')]");
            if (docDetailsNode != null)
            {
                var h1InDetails = docDetailsNode.SelectSingleNode(".//h1");
                if (h1InDetails != null)
                {
                    var h1Text = h1InDetails.InnerText;
                    // Extract alphanumeric sequence (invoice number)
                    var numberMatch = Regex.Match(h1Text, @"([A-Za-z0-9\-]+)");
                    if (numberMatch.Success && !h1Text.Contains("REIKNINGUR", StringComparison.OrdinalIgnoreCase))
                    {
                        invoice.InvoiceNumber = numberMatch.Groups[1].Value.Trim();
                    }
                }
            }
        }

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

        // 1.5. Extract Buyer (Kaupandi) and TaxId
        string? buyerSectionText = null;
        
        // Try new format first: buyer_info class
        var buyerInfoNode = doc.DocumentNode.SelectSingleNode("//*[contains(@class, 'buyer_info')]");
        if (buyerInfoNode != null)
        {
            buyerSectionText = buyerInfoNode.InnerText;
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
                        buyerSectionText = parent.InnerText;
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
                            buyerSectionText = line;
                            break;
                        }
                    }
                }
            }
        }
        
        // Extract TaxId (Icelandic kennitala: 10 digits, may have dash)
        if (!string.IsNullOrEmpty(buyerSectionText))
        {
            var taxIdMatch = Regex.Match(buyerSectionText, @"\b(\d{6}[-]?\d{4})\b");
            if (taxIdMatch.Success)
            {
                invoice.BuyerTaxId = taxIdMatch.Groups[1].Value.Replace("-", ""); // Remove dash if present
            }
        }
        
        // Also try to find TaxId in UBLID spans or similar
        if (string.IsNullOrEmpty(invoice.BuyerTaxId))
        {
            var ublIdNode = doc.DocumentNode.SelectSingleNode("//*[contains(@class, 'UBLID')]");
            if (ublIdNode != null)
            {
                var taxIdMatch = Regex.Match(ublIdNode.InnerText, @"\b(\d{6}[-]?\d{4})\b");
                if (taxIdMatch.Success)
                {
                    invoice.BuyerTaxId = taxIdMatch.Groups[1].Value.Replace("-", "");
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


        // 3. Extract Totals (with multiple fallback strategies)
        invoice.TotalAmount = ExtractTotalAmount(doc);

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

        // 5. Final fallback: Calculate total from items if total is still 0
        if (invoice.TotalAmount == 0 && invoice.Items != null && invoice.Items.Any())
        {
            // Sum up TotalPriceWithVat if available, otherwise sum TotalPrice
            invoice.TotalAmount = invoice.Items.Sum(item => 
                item.TotalPriceWithVat > 0 ? item.TotalPriceWithVat : item.TotalPrice);
        }

        return invoice;
    }

    private bool IsDecimal(string input)
    {
         var clean = input.Trim().Replace(".", "").Replace(",", ".");
         return decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out _);
    }

    private decimal ExtractTotalAmount(HtmlDocument doc)
    {
        // Strategy 1: Try payable_amount class (most common in newer invoices)
        var totalNode = doc.DocumentNode.SelectSingleNode("//*[contains(@class, 'payable_amount')]");
        if (totalNode != null)
        {
            var amount = ParseDecimal(totalNode.InnerText);
            if (amount > 0) return amount;
        }

        // Strategy 2: Try various total labels (final total with VAT) - this is the actual total
        var totalLabelPatterns = new[]
        {
            "Samtala reiknings",
            "Upphæð reiknings",
            "Til greiðslu"
        };

        foreach (var pattern in totalLabelPatterns)
        {
            var totalNodes = doc.DocumentNode.SelectNodes($"//*[text()[contains(., '{pattern}') or contains(., '{pattern}:')]]");
            if (totalNodes != null)
            {
                foreach (var node in totalNodes)
                {
                    // Look for the amount in the same row/container
                    var parent = node.ParentNode;
                    if (parent != null)
                    {
                        // Try to find a number in the parent's text (usually in a <b> tag or adjacent cell)
                        var parentText = parent.InnerText;
                        var amount = ParseDecimal(parentText);
                        if (amount > 0) return amount;
                        
                        // Also check sibling nodes (for table cells)
                        var siblings = parent.SelectNodes(".//td | .//b | .//p");
                        if (siblings != null)
                        {
                            foreach (var sibling in siblings)
                            {
                                var siblingText = sibling.InnerText;
                                if (IsDecimal(siblingText))
                                {
                                    amount = ParseDecimal(siblingText);
                                    if (amount > 0) return amount;
                                }
                            }
                        }
                        
                        // Check following siblings (for row-based layouts)
                        var nextSibling = parent.NextSibling;
                        while (nextSibling != null)
                        {
                            if (nextSibling.NodeType == HtmlAgilityPack.HtmlNodeType.Element)
                            {
                                var siblingAmount = ParseDecimal(nextSibling.InnerText);
                                if (siblingAmount > 0) return siblingAmount;
                            }
                            nextSibling = nextSibling.NextSibling;
                        }
                    }
                }
            }
        }

        // Strategy 3: Calculate from "Samtals:" (net) + "Samtals VSK:" (VAT)
        // Look in upphaedsamantekt table
        var upphaedTable = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'upphaedsamantekt')]");
        if (upphaedTable != null)
        {
            decimal netTotal = 0;
            decimal vatTotal = 0;
            
            // Find "Samtals:" row
            var samtalsNodes = upphaedTable.SelectNodes(".//*[text()[contains(., 'Samtals:') and not(contains(., 'VSK'))]]");
            if (samtalsNodes != null)
            {
                foreach (var node in samtalsNodes)
                {
                    var parent = node.ParentNode;
                    if (parent != null)
                    {
                        var cells = parent.SelectNodes(".//td");
                        if (cells != null && cells.Count >= 3)
                        {
                            // Amount is usually in the 3rd cell
                            netTotal = ParseDecimal(cells[2].InnerText);
                            if (netTotal > 0) break;
                        }
                    }
                }
            }
            
            // Find "Samtals VSK:" row
            var vskNodes = upphaedTable.SelectNodes(".//*[text()[contains(., 'Samtals VSK:')]]");
            if (vskNodes != null)
            {
                foreach (var node in vskNodes)
                {
                    var parent = node.ParentNode;
                    if (parent != null)
                    {
                        var cells = parent.SelectNodes(".//td");
                        if (cells != null && cells.Count >= 3)
                        {
                            vatTotal = ParseDecimal(cells[2].InnerText);
                            if (vatTotal > 0) break;
                        }
                        else
                        {
                            // Sometimes VAT is in the same node text
                            vatTotal = ParseDecimal(parent.InnerText);
                            if (vatTotal > 0) break;
                        }
                    }
                }
            }
            
            if (netTotal > 0 && vatTotal > 0)
            {
                return netTotal + vatTotal;
            }
            else if (netTotal > 0)
            {
                // If we only have net total, return it (better than 0)
                return netTotal;
            }
        }

        // Strategy 4: Try finding "Samtals:" in any table and extract the largest number
        var allSamtalsNodes = doc.DocumentNode.SelectNodes("//*[text()[contains(., 'Samtals')]]");
        if (allSamtalsNodes != null)
        {
            decimal maxAmount = 0;
            foreach (var node in allSamtalsNodes)
            {
                var parent = node.ParentNode;
                if (parent != null)
                {
                    var cells = parent.SelectNodes(".//td");
                    if (cells != null)
                    {
                        foreach (var cell in cells)
                        {
                            var amount = ParseDecimal(cell.InnerText);
                            if (amount > maxAmount) maxAmount = amount;
                        }
                    }
                    else
                    {
                        var amount = ParseDecimal(parent.InnerText);
                        if (amount > maxAmount) maxAmount = amount;
                    }
                }
            }
            if (maxAmount > 0) return maxAmount;
        }

        // Strategy 5: Try text-based search for various total patterns
        var allText = doc.DocumentNode.InnerText;
        var textLines = allText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(l => l.Trim())
                           .Where(l => !string.IsNullOrWhiteSpace(l))
                           .ToList();
        foreach (var line in textLines)
        {
            if (line.Contains("Samtala reiknings", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Upphæð reiknings", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Til greiðslu", StringComparison.OrdinalIgnoreCase))
            {
                var amount = ParseDecimal(line);
                if (amount > 0) return amount;
            }
        }

        // Strategy 6: Calculate from items if available (will be done after items are parsed)
        // This will be handled as a final fallback after items are parsed

        return 0; // Will be recalculated from items if available
    }

    private decimal ParseDecimal(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return 0;
        
        // Decode HTML entities (e.g., &nbsp; becomes space)
        input = System.Net.WebUtility.HtmlDecode(input);
        
        // Remove common currency symbols and text
        input = Regex.Replace(input, @"\b(ISK|kr|EUR|USD)\b", "", RegexOptions.IgnoreCase);
        
        // Find all potential numbers (Icelandic format: digits with dots and commas)
        // Pattern: matches numbers like 137.214,00 or 1234,56 or 1000
        var matches = Regex.Matches(input, @"\d{1,3}(?:\.\d{3})*(?:,\d{2})?|\d+(?:,\d{2})?");
        
        if (matches.Count == 0) return 0;
        
        // If multiple matches, prefer the largest one (likely the total)
        // Also prefer matches with decimal places (more likely to be amounts)
        decimal maxAmount = 0;
        decimal maxAmountWithDecimals = 0;
        
        foreach (Match match in matches)
        {
            var valStr = match.Value;
            
            // Icelandic: 1.000,00 -> remove dots, repl comma with dot
            var clean = valStr.Replace(".", "").Replace(",", ".");
            
            if (decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            {
                if (result > maxAmount) maxAmount = result;
                
                // Prefer numbers with decimal places (more likely to be currency amounts)
                if (valStr.Contains(",") && result > maxAmountWithDecimals)
                {
                    maxAmountWithDecimals = result;
                }
            }
        }
        
        // Return the amount with decimals if found, otherwise the max amount
        return maxAmountWithDecimals > 0 ? maxAmountWithDecimals : maxAmount;
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
