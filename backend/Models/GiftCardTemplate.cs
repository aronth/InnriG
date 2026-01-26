namespace InnriGreifi.API.Models;

public class GiftCardTemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? DefaultAmount { get; set; }
    public string? MessageTemplate { get; set; }
    public string? AmountAsText { get; set; }
    public bool IsMonetaryTemplate { get; set; } = true;
    public Guid? RestaurantId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Restaurant? Restaurant { get; set; }
    public List<GiftCard> GiftCards { get; set; } = new();
}



