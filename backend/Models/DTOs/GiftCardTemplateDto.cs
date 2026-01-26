namespace InnriGreifi.API.Models.DTOs;

public class GiftCardTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? DefaultAmount { get; set; }
    public string? MessageTemplate { get; set; }
    public string? AmountAsText { get; set; }
    public bool IsMonetaryTemplate { get; set; }
    public Guid? RestaurantId { get; set; }
    public string? RestaurantName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateGiftCardTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? DefaultAmount { get; set; }
    public string? MessageTemplate { get; set; }
    public string? AmountAsText { get; set; }
    public bool IsMonetaryTemplate { get; set; } = true;
    public Guid? RestaurantId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateGiftCardTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? DefaultAmount { get; set; }
    public string? MessageTemplate { get; set; }
    public string? AmountAsText { get; set; }
    public bool IsMonetaryTemplate { get; set; }
    public Guid? RestaurantId { get; set; }
    public bool IsActive { get; set; }
}



