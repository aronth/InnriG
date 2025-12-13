using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InnriGreifi.API.Models;

public class InvoiceItem
{
    public Guid Id { get; set; }
    
    public Guid InvoiceId { get; set; }
    
    public Guid ProductId { get; set; }
    
    public string ItemName { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty; // Product Code
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; } // Discounted Price
    public decimal ListPrice { get; set; } // Original Price
    public string VatCode { get; set; } = string.Empty; // AA, S, etc.
    public decimal Discount { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal TotalPriceWithVat { get; set; } // Optional, based on CSV
    
    // Navigation properties
    [JsonIgnore]
    public Invoice Invoice { get; set; } = null!;
    [JsonIgnore]
    public Product Product { get; set; } = null!;
}
