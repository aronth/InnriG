using InnriGreifi.API.Models;
using InnriGreifi.API.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnriGreifi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserManager<User> _userManager;

    public UsersController(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userManager.Users
            .OrderBy(u => u.Name)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.UserName ?? string.Empty,
                Name = u.Name,
                MustChangePassword = u.MustChangePassword,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return NotFound();

        var userDto = new UserDto
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            Name = user.Name,
            MustChangePassword = user.MustChangePassword,
            CreatedAt = user.CreatedAt
        };

        return Ok(userDto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] CreateUserDto updateUserDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return NotFound();

        // Update name only (username is immutable)
        user.Name = updateUserDto.Name;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        var userDto = new UserDto
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            Name = user.Name,
            MustChangePassword = user.MustChangePassword,
            CreatedAt = user.CreatedAt
        };

        return Ok(userDto);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
            return NotFound();

        // Prevent deleting yourself
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser != null && currentUser.Id == id)
            return BadRequest("Cannot delete your own account");

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        return NoContent();
    }
}

