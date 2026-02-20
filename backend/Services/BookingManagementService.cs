using InnriGreifi.API.Data;
using InnriGreifi.API.Models;
using InnriGreifi.API.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace InnriGreifi.API.Services;

public class BookingManagementService : IBookingManagementService
{
    private readonly AppDbContext _context;

    public BookingManagementService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<BookingManagementDto>> GetBookingsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        Guid? customerId = null,
        Guid? locationId = null,
        string? status = null)
    {
        var query = _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.Location)
            .Include(b => b.BookingMenuItems)
                .ThenInclude(bmi => bmi.MenuItem)
            .AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(b => b.BookingDate >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            query = query.Where(b => b.BookingDate <= toDate.Value.Date);
        }

        if (customerId.HasValue)
        {
            query = query.Where(b => b.CustomerId == customerId.Value);
        }

        if (locationId.HasValue)
        {
            query = query.Where(b => b.LocationId == locationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(b => b.Status == status);
        }

        var bookings = await query
            .OrderBy(b => b.BookingDate)
            .ThenBy(b => b.StartTime)
            .ToListAsync();

        return bookings.Select(MapToDto).ToList();
    }

    public async Task<BookingManagementDto?> GetBookingByIdAsync(Guid id)
    {
        var booking = await _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.Location)
            .Include(b => b.BookingMenuItems)
                .ThenInclude(bmi => bmi.MenuItem)
            .FirstOrDefaultAsync(b => b.Id == id);

        return booking == null ? null : MapToDto(booking);
    }

    public async Task<BookingManagementDto> CreateBookingAsync(CreateBookingDto dto)
    {
        // Verify customer exists
        var customer = await _context.Customers.FindAsync(dto.CustomerId);
        if (customer == null)
            throw new ArgumentException("Customer not found", nameof(dto.CustomerId));

        // Verify location exists if provided
        if (dto.LocationId.HasValue)
        {
            var location = await _context.Restaurants.FindAsync(dto.LocationId.Value);
            if (location == null)
                throw new ArgumentException("Location not found", nameof(dto.LocationId));
        }

        // Ensure BookingDate is UTC (PostgreSQL requires UTC for timestamp with time zone)
        var bookingDateUtc = dto.BookingDate.Kind == DateTimeKind.Utc
            ? dto.BookingDate.Date
            : DateTime.SpecifyKind(dto.BookingDate.Date, DateTimeKind.Utc);

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            CustomerId = dto.CustomerId,
            LocationId = dto.LocationId,
            BookingDate = bookingDateUtc,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            AdultCount = dto.AdultCount,
            ChildCount = dto.ChildCount,
            Status = dto.Status,
            SpecialRequests = dto.SpecialRequests,
            Notes = dto.Notes,
            NeedsPrint = dto.NeedsPrint,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Bookings.Add(booking);

        // Add menu items
        foreach (var itemDto in dto.MenuItems)
        {
            var menuItem = await _context.MenuItems.FindAsync(itemDto.MenuItemId);
            if (menuItem == null)
                throw new ArgumentException($"MenuItem {itemDto.MenuItemId} not found");

            var bookingMenuItem = new BookingMenuItem
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                MenuItemId = itemDto.MenuItemId,
                Quantity = itemDto.Quantity,
                UnitPrice = itemDto.UnitPrice ?? menuItem.Price,
                Notes = itemDto.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BookingMenuItems.Add(bookingMenuItem);
        }

        await _context.SaveChangesAsync();

        return await GetBookingByIdAsync(booking.Id) ?? throw new InvalidOperationException("Failed to retrieve created booking");
    }

    public async Task<BookingManagementDto?> UpdateBookingAsync(Guid id, UpdateBookingDto dto)
    {
        var booking = await _context.Bookings
            .Include(b => b.BookingMenuItems)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null)
            return null;

        // Verify customer exists
        var customer = await _context.Customers.FindAsync(dto.CustomerId);
        if (customer == null)
            throw new ArgumentException("Customer not found", nameof(dto.CustomerId));

        // Verify location exists if provided
        if (dto.LocationId.HasValue)
        {
            var location = await _context.Restaurants.FindAsync(dto.LocationId.Value);
            if (location == null)
                throw new ArgumentException("Location not found", nameof(dto.LocationId));
        }

        // Ensure BookingDate is UTC (PostgreSQL requires UTC for timestamp with time zone)
        var bookingDateUtc = dto.BookingDate.Kind == DateTimeKind.Utc
            ? dto.BookingDate.Date
            : DateTime.SpecifyKind(dto.BookingDate.Date, DateTimeKind.Utc);

        booking.CustomerId = dto.CustomerId;
        booking.LocationId = dto.LocationId;
        booking.BookingDate = bookingDateUtc;
        booking.StartTime = dto.StartTime;
        booking.EndTime = dto.EndTime;
        booking.AdultCount = dto.AdultCount;
        booking.ChildCount = dto.ChildCount;
        booking.Status = dto.Status;
        booking.SpecialRequests = dto.SpecialRequests;
        booking.Notes = dto.Notes;
        booking.NeedsPrint = dto.NeedsPrint;
        booking.UpdatedAt = DateTime.UtcNow;

        // Remove existing menu items
        _context.BookingMenuItems.RemoveRange(booking.BookingMenuItems);

        // Add new menu items
        foreach (var itemDto in dto.MenuItems)
        {
            var menuItem = await _context.MenuItems.FindAsync(itemDto.MenuItemId);
            if (menuItem == null)
                throw new ArgumentException($"MenuItem {itemDto.MenuItemId} not found");

            var bookingMenuItem = new BookingMenuItem
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                MenuItemId = itemDto.MenuItemId,
                Quantity = itemDto.Quantity,
                UnitPrice = itemDto.UnitPrice ?? menuItem.Price,
                Notes = itemDto.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BookingMenuItems.Add(bookingMenuItem);
        }

        await _context.SaveChangesAsync();

        return await GetBookingByIdAsync(id);
    }

    public async Task<bool> DeleteBookingAsync(Guid id)
    {
        var booking = await _context.Bookings.FindAsync(id);
        if (booking == null)
            return false;

        _context.Bookings.Remove(booking);
        await _context.SaveChangesAsync();

        return true;
    }

    private static BookingManagementDto MapToDto(Booking booking)
    {
        return new BookingManagementDto
        {
            Id = booking.Id,
            CustomerId = booking.CustomerId,
            Customer = booking.Customer != null ? new CustomerDto
            {
                Id = booking.Customer.Id,
                Name = booking.Customer.Name,
                Phone = booking.Customer.Phone,
                Email = booking.Customer.Email,
                Notes = booking.Customer.Notes,
                CreatedAt = booking.Customer.CreatedAt,
                UpdatedAt = booking.Customer.UpdatedAt
            } : null,
            LocationId = booking.LocationId,
            LocationName = booking.Location?.Name,
            BookingDate = booking.BookingDate,
            StartTime = booking.StartTime,
            EndTime = booking.EndTime,
            AdultCount = booking.AdultCount,
            ChildCount = booking.ChildCount,
            Status = booking.Status,
            SpecialRequests = booking.SpecialRequests,
            Notes = booking.Notes,
            NeedsPrint = booking.NeedsPrint,
            MenuItems = booking.BookingMenuItems.Select(bmi => new BookingMenuItemDto
            {
                Id = bmi.Id,
                MenuItemId = bmi.MenuItemId,
                MenuItemName = bmi.MenuItem?.Name ?? string.Empty,
                Quantity = bmi.Quantity,
                UnitPrice = bmi.UnitPrice,
                Notes = bmi.Notes
            }).ToList(),
            CreatedAt = booking.CreatedAt,
            UpdatedAt = booking.UpdatedAt
        };
    }
}

