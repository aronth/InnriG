namespace InnriGreifi.API.Models.DTOs;

public class CreateGiftCardDto
{
    public Guid? TemplateId { get; set; }
    public Guid? RestaurantId { get; set; }
    public decimal Amount { get; set; }
    public string? Message { get; set; }
    public string? DkNumber { get; set; }
    public bool PrintWithBackground { get; set; } = false;
}

public class CreateGiftCardBatchDto
{
    public int Count { get; set; } = 1;
    public Guid? TemplateId { get; set; }
    public Guid? RestaurantId { get; set; }
    public decimal? Amount { get; set; }
    public string? Message { get; set; }
    public bool PrintWithBackground { get; set; } = false;
}



