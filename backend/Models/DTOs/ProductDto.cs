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

public class PaginatedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

public class ProductLookupDto
{
    public Guid Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public decimal? LatestPrice { get; set; }
}

public class MostOrderedProductDto
{
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public decimal TotalQuantity { get; set; }
    public int OrderCount { get; set; } // Number of times this product was ordered
    public decimal? AverageUnitPrice { get; set; }
    public decimal? LatestUnitPrice { get; set; }
}