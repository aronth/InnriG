namespace InnriGreifi.API.Models.DTOs;

public class ProductListDto
{
    public Guid Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CurrentUnit { get; set; }
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    
    // Latest price information
    public decimal? LatestPrice { get; set; }
    public decimal? ListPrice { get; set; }
    public decimal? Discount { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public DateTime? LastPurchaseDate { get; set; }
}
