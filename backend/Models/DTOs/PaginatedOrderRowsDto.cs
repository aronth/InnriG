using InnriGreifi.API.Models;

namespace InnriGreifi.API.Models.DTOs;

public class PaginatedOrderRowsDto
{
    public List<OrderRow> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
    public int Page => Take > 0 ? (Skip / Take) + 1 : 1;
    public int TotalPages => Take > 0 ? (int)Math.Ceiling((double)TotalCount / Take) : 1;
}

