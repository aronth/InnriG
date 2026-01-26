using System.ComponentModel.DataAnnotations.Schema;

namespace InnriGreifi.API.Models;

public class GiftCard
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public Guid? TemplateId { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    
    public string? Message { get; set; }
    public Guid? RestaurantId { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? DkNumber { get; set; }
    public GiftCardStatus Status { get; set; } = GiftCardStatus.Created;
    public Guid? CreatedById { get; set; }
    public DateTime? SoldAt { get; set; }
    public DateTime? RedeemedAt { get; set; }
    public bool PrintWithBackground { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public GiftCardTemplate? Template { get; set; }
    public Restaurant? Restaurant { get; set; }
    public User? CreatedBy { get; set; }
}



