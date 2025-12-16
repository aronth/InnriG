namespace InnriGreifi.API.Models.DTOs;

public class PriceComparisonDto
{
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    
    // From Date
    public DateTime FromDate { get; set; }
    public decimal FromUnitPrice { get; set; }
    public decimal FromListPrice { get; set; }
    public decimal FromDiscount { get; set; }
    public string FromInvoiceNumber { get; set; } = string.Empty;
    
    // To Date
    public DateTime ToDate { get; set; }
    public decimal ToUnitPrice { get; set; }
    public decimal ToListPrice { get; set; }
    public decimal ToDiscount { get; set; }
    public string ToInvoiceNumber { get; set; } = string.Empty;
    
    // Changes
    public decimal UnitPriceChange { get; set; }
    public decimal UnitPriceChangePercent { get; set; }
    public decimal ListPriceChange { get; set; }
    public decimal ListPriceChangePercent { get; set; }
    public decimal DiscountChange { get; set; }
    public decimal DiscountChangePercent { get; set; }
}

public class PriceComparisonSummaryDto
{
    public int TotalProducts { get; set; }
    public int ProductsWithPriceIncrease { get; set; }
    public int ProductsWithPriceDecrease { get; set; }
    public int ProductsWithNoChange { get; set; }
    public decimal AveragePriceChangePercent { get; set; }
    public decimal AveragePriceIncrease { get; set; }
    public decimal AveragePriceDecrease { get; set; }
}

