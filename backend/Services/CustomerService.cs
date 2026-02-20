using InnriGreifi.API.Data;
using InnriGreifi.API.Models;
using InnriGreifi.API.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace InnriGreifi.API.Services;

public class CustomerService : ICustomerService
{
    private readonly AppDbContext _context;

    public CustomerService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<CustomerDto>> GetAllCustomersAsync()
    {
        var customers = await _context.Customers
            .OrderBy(c => c.Name)
            .ToListAsync();

        return customers.Select(MapToDto).ToList();
    }

    public async Task<CustomerDto?> GetCustomerByIdAsync(Guid id)
    {
        var customer = await _context.Customers.FindAsync(id);
        return customer == null ? null : MapToDto(customer);
    }

    public async Task<CustomerDto?> GetCustomerByPhoneAsync(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return null;

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Phone == phone);

        return customer == null ? null : MapToDto(customer);
    }

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerDto dto)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Phone = dto.Phone,
            Email = dto.Email,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        return MapToDto(customer);
    }

    public async Task<CustomerDto?> UpdateCustomerAsync(Guid id, UpdateCustomerDto dto)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null)
            return null;

        customer.Name = dto.Name;
        customer.Phone = dto.Phone;
        customer.Email = dto.Email;
        customer.Notes = dto.Notes;
        customer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(customer);
    }

    public async Task<bool> DeleteCustomerAsync(Guid id)
    {
        var customer = await _context.Customers
            .Include(c => c.Bookings)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (customer == null)
            return false;

        // Check if customer has bookings
        if (customer.Bookings.Any())
        {
            throw new InvalidOperationException("Cannot delete customer with existing bookings");
        }

        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();

        return true;
    }

    private static CustomerDto MapToDto(Customer customer)
    {
        return new CustomerDto
        {
            Id = customer.Id,
            Name = customer.Name,
            Phone = customer.Phone,
            Email = customer.Email,
            Notes = customer.Notes,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt
        };
    }
}

