namespace InnriGreifi.API.Models.DTOs;

public class GiftCardDto
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public Guid? TemplateId { get; set; }
    public string? TemplateName { get; set; }
    public Guid? RestaurantId { get; set; }
    public string? RestaurantName { get; set; }
    public string? RestaurantCode { get; set; }
    public decimal Amount { get; set; }
    public string? Message { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? DkNumber { get; set; }
    public string Status { get; set; } = "Created";
    public Guid? CreatedById { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? SoldAt { get; set; }
    public DateTime? RedeemedAt { get; set; }
    public bool PrintWithBackground { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}



