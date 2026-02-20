using InnriGreifi.API.Models.DTOs;
using InnriGreifi.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InnriGreifi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "User,Manager,Admin")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(ICustomerService customerService, ILogger<CustomersController> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCustomers()
    {
        try
        {
            var customers = await _customerService.GetAllCustomersAsync();
            return Ok(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching customers");
            return StatusCode(500, new { error = "Error fetching customers", details = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCustomer(Guid id)
    {
        try
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null)
                return NotFound();

            return Ok(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching customer {CustomerId}", id);
            return StatusCode(500, new { error = "Error fetching customer", details = ex.Message });
        }
    }

    [HttpGet("phone/{phone}")]
    public async Task<IActionResult> GetCustomerByPhone(string phone)
    {
        try
        {
            var customer = await _customerService.GetCustomerByPhoneAsync(phone);
            if (customer == null)
                return NotFound();

            return Ok(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching customer by phone");
            return StatusCode(500, new { error = "Error fetching customer", details = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var customer = await _customerService.CreateCustomerAsync(dto);
            return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            return StatusCode(500, new { error = "Error creating customer", details = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> UpdateCustomer(Guid id, [FromBody] UpdateCustomerDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var customer = await _customerService.UpdateCustomerAsync(id, dto);
            if (customer == null)
                return NotFound();

            return Ok(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer {CustomerId}", id);
            return StatusCode(500, new { error = "Error updating customer", details = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> DeleteCustomer(Guid id)
    {
        try
        {
            var deleted = await _customerService.DeleteCustomerAsync(id);
            if (!deleted)
                return NotFound();

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer {CustomerId}", id);
            return StatusCode(500, new { error = "Error deleting customer", details = ex.Message });
        }
    }
}

