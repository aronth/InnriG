using System.ComponentModel.DataAnnotations;

namespace InnriGreifi.API.Models;

public class Product
{
    public Guid Id { get; set; }
    
    [Required]
    public Guid SupplierId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string ProductCode { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(300)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [MaxLength(20)]
    public string? CurrentUnit { get; set; }

    /// <summary>Multiplier to convert unit price to normalized unit price: normalizedPrice = unitPrice * NormalizedUnitMultiplier (e.g. 0.1 for 10 kg per sold unit).</summary>
    public decimal? NormalizedUnitMultiplier { get; set; }

    /// <summary>Base unit for comparison: kg, L, m, stk.</summary>
    [MaxLength(10)]
    public string? NormalizedBaseUnit { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Supplier Supplier { get; set; } = null!;
    public List<InvoiceItem> InvoiceItems { get; set; } = new();
}
