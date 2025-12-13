using System.ComponentModel.DataAnnotations;

namespace InnriGreifi.API.Models;

public class Supplier
{
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? ContactInfo { get; set; }
    
    [MaxLength(500)]
    public string? Address { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public List<Invoice> Invoices { get; set; } = new();
    public List<Product> Products { get; set; } = new();
}
