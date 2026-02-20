using InnriGreifi.API.Models.DTOs;

namespace InnriGreifi.API.Services;

public interface ICustomerService
{
    Task<List<CustomerDto>> GetAllCustomersAsync();
    Task<CustomerDto?> GetCustomerByIdAsync(Guid id);
    Task<CustomerDto?> GetCustomerByPhoneAsync(string phone);
    Task<CustomerDto> CreateCustomerAsync(CreateCustomerDto dto);
    Task<CustomerDto?> UpdateCustomerAsync(Guid id, UpdateCustomerDto dto);
    Task<bool> DeleteCustomerAsync(Guid id);
}

