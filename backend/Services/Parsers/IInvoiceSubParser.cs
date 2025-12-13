using HtmlAgilityPack;
using InnriGreifi.API.Models;

namespace InnriGreifi.API.Services.Parsers;

public interface IInvoiceSubParser
{
    // Returns true if this parser can handle the invoice (based on supplier or HTML structure)
    bool CanParse(string supplierName, HtmlDocument doc);
    
    // Parses the line items
    List<InvoiceItem> ParseItems(HtmlDocument doc, Guid invoiceId);
}
