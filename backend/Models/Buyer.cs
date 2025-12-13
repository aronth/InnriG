using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InnriGreifi.API.Models;

public class Buyer
{
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(300)]
    public string Name { get; set; } = string.Empty;
    
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? TaxId { get; set; } // Kennitala/VAT number
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    [JsonIgnore]
    public List<Invoice> Invoices { get; set; } = new();
}

