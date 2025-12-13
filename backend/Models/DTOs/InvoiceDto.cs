namespace InnriGreifi.API.Models.DTOs;

public class InvoiceConfirmDto
{
    public Guid Id { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string BuyerName { get; set; } = string.Empty;
    public string BuyerTaxId { get; set; } = string.Empty; // SSN/Kennitala - Required for buyer identification
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public decimal TotalAmount { get; set; }
    public List<InvoiceItemDto> Items { get; set; } = new();
}

public class InvoiceItemDto
{
    public Guid Id { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty; // Product Code
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal ListPrice { get; set; }
    public string VatCode { get; set; } = string.Empty;
    public decimal Discount { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal TotalPriceWithVat { get; set; }
}
