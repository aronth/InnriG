using InnriGreifi.API.Models;

namespace InnriGreifi.API.Services;

public interface IInvoiceParser
{
    Invoice ParseInvoice(string htmlContent, string fileName);
}
