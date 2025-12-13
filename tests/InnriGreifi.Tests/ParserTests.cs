using Xunit;
using InnriGreifi.API.Services;
using InnriGreifi.API.Models;
using System.IO;
using System.Linq;

namespace InnriGreifi.Tests;

public class ParserTests
{
    [Fact]
    public void ParseInvoice_ShouldExtractItems_FromDivBasedLayout()
    {
        // Arrange
        var parser = new HtmlInvoiceParser();
        var filePath = Path.Combine("..", "..", "..", "..", "..", "example_invoices", "Reikn-0019064.html");
        
        // Ensure path exists (absolute path fallback for safety in test runner)
        if (!File.Exists(filePath))
        {
            filePath = @"c:\Users\Aron\Development\InnriGreifi\example_invoices\Reikn-0019064.html";
        }

        var htmlContent = File.ReadAllText(filePath);

        // Act
        var invoice = parser.ParseInvoice(htmlContent, "Reikn-0019064.html");

        // Assert
        Assert.NotNull(invoice);
        Assert.NotNull(invoice.Items);
        Assert.NotEmpty(invoice.Items);
        
        // Verify specific items
        // Item 1: 6311, Rækja 16/20, 10.00 KGM, 1.990,00, Total 19.900,00
        var item1 = invoice.Items.FirstOrDefault(i => i.ItemId == "6311");
        Assert.NotNull(item1);
        Assert.Contains("Rækja", item1.ItemName);
        Assert.Equal(10.00m, item1.Quantity);
        Assert.Equal("KGM", item1.Unit);
        Assert.Equal(1990.00m, item1.UnitPrice);
        Assert.Equal(1990.00m, item1.ListPrice); // No discount, so same
        Assert.Equal("S11", item1.VatCode);
        Assert.Equal(19900.00m, item1.TotalPrice);

        // Item 2: 421, Humar
        var item2 = invoice.Items.FirstOrDefault(i => i.ItemId == "421");
        Assert.NotNull(item2);
        Assert.Equal(10.00m, item2.Quantity);
        Assert.Equal(3390.00m, item2.UnitPrice);

        // Check total count
        // 6311, 421, 6040, 6296 -> 4 items
        Assert.Equal(4, invoice.Items.Count);
    }
    [Fact]
    public void ParseInvoice_ShouldExtractItems_FromTableBasedLayout()
    {
        // Arrange
        var parser = new HtmlInvoiceParser();
        var filePath = @"c:\Users\Aron\Development\InnriGreifi\example_invoices\Reikn-761633.html";

        // Fallback for relative path if needed but absolute is safer for this environment
        if (!File.Exists(filePath))
        {
             // Try relative just in case
             filePath = Path.Combine("..", "..", "..", "..", "..", "example_invoices", "Reikn-761633.html");
        }
        
        var htmlContent = File.ReadAllText(filePath);

        // Act
        var invoice = parser.ParseInvoice(htmlContent, "Reikn-761633.html");

        // Assert
        Assert.NotNull(invoice);
        Assert.NotEmpty(invoice.Items);
        
        // Verify Item 1: 1680, Soðið hangilæri, 6.60 KGM, 6.353,60, Total 41.933,76
        var item1 = invoice.Items.FirstOrDefault(i => i.ItemId == "1680");
        Assert.NotNull(item1);
        Assert.Contains("hangilæri", item1.ItemName);
        Assert.Equal(6.60m, item1.Quantity);
        Assert.Equal("KGM", item1.Unit);
        Assert.Equal(6353.60m, item1.UnitPrice);
        Assert.Equal(7942.00m, item1.ListPrice); // From detailed table
        Assert.Equal(20.00m, item1.Discount); // From detailed table
        Assert.Equal("AA", item1.VatCode);
        Assert.Equal(41933.76m, item1.TotalPrice); // 6.6 * 6353.6 = 41933.76

        Assert.Single(invoice.Items); // This invoice usually has 1 item shown in snippet
    }
}
