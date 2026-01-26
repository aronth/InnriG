namespace InnriGreifi.API.Models.DTOs;

public class GiftCardPreviewDto
{
    public Guid? TemplateId { get; set; }
    public decimal Amount { get; set; }
    public string? Message { get; set; }
    public Guid? RestaurantId { get; set; }
    public string? DkNumber { get; set; }
    public bool PrintWithBackground { get; set; }
}

