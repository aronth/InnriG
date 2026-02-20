namespace InnriGreifi.API.Models.DTOs;

public class BookingManagementDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public CustomerDto? Customer { get; set; }
    public Guid? LocationId { get; set; }
    public string? LocationName { get; set; }
    public DateTime BookingDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public int AdultCount { get; set; }
    public int ChildCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? SpecialRequests { get; set; }
    public string? Notes { get; set; }
    public bool NeedsPrint { get; set; }
    public List<BookingMenuItemDto> MenuItems { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class BookingMenuItemDto
{
    public Guid Id { get; set; }
    public Guid MenuItemId { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Notes { get; set; }
}

public class CreateBookingDto
{
    public Guid CustomerId { get; set; }
    public Guid? LocationId { get; set; }
    public DateTime BookingDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public int AdultCount { get; set; }
    public int ChildCount { get; set; }
    public string Status { get; set; } = "Ný";
    public string? SpecialRequests { get; set; }
    public string? Notes { get; set; }
    public bool NeedsPrint { get; set; }
    public List<CreateBookingMenuItemDto> MenuItems { get; set; } = new();
}

public class CreateBookingMenuItemDto
{
    public Guid MenuItemId { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal? UnitPrice { get; set; } // If null, use MenuItem's current price
    public string? Notes { get; set; }
}

public class UpdateBookingDto
{
    public Guid CustomerId { get; set; }
    public Guid? LocationId { get; set; }
    public DateTime BookingDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public int AdultCount { get; set; }
    public int ChildCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? SpecialRequests { get; set; }
    public string? Notes { get; set; }
    public bool NeedsPrint { get; set; }
    public List<CreateBookingMenuItemDto> MenuItems { get; set; } = new();
}

