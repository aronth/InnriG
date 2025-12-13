using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InnriGreifi.API.Models;

public class Invoice
{
    public Guid Id { get; set; }
    
    public Guid SupplierId { get; set; }
    
    public string SupplierName { get; set; } = string.Empty; // Keep temporarily for migration
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    [JsonIgnore]
    public Supplier Supplier { get; set; } = null!;
    public List<InvoiceItem> Items { get; set; } = new();
}
