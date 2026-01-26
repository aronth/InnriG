namespace InnriGreifi.API.Models.DTOs;

public class GreifinnOrderDto
{
    public string OrderId { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerAddress { get; set; }
    public DateTime? AddedDate { get; set; }
    public int? LocationId { get; set; }
    public string? LocationName { get; set; }
    public int? DeliveryMethodId { get; set; }
    public string? DeliveryMethodName { get; set; }
    public int? PaymentMethodId { get; set; }
    public string? PaymentMethodName { get; set; }
    public decimal? TotalPrice { get; set; }
    public string? ExternalId { get; set; }
    public int? StatusId { get; set; }
    public string? StatusName { get; set; }
    public string? DetailUrl { get; set; }
}

public class GreifinnOrderListDto
{
    public List<GreifinnOrderDto> Orders { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasMorePages { get; set; }
}

public class GreifinnOrderItemDto
{
    public string Name { get; set; } = string.Empty;
    public string? ItemId { get; set; }
    public List<GreifinnOrderOptionDto> Options { get; set; } = new();
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class GreifinnOrderOptionDto
{
    public string Name { get; set; } = string.Empty;
    public string? OptionId { get; set; }
    public string? OptionGroupId { get; set; }
    public decimal Price { get; set; }
}

public class GreifinnOrderDetailDto
{
    public string OrderId { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerAddress { get; set; }
    public string? DeliveryMethod { get; set; }
    public string? PaymentMethod { get; set; }
    public DateTime? ReadyTime { get; set; }
    public DateTime? AddedTime { get; set; }
    public List<GreifinnOrderItemDto> Items { get; set; } = new();
    public decimal TotalPrice { get; set; }
}

