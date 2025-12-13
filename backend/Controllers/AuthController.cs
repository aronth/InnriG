using InnriGreifi.API.Models;
using InnriGreifi.API.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace InnriGreifi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;

    public AuthController(UserManager<User> userManager, SignInManager<User> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.FindByNameAsync(loginDto.Username);
        if (user == null)
            return Unauthorized("Invalid username or password");

        var result = await _signInManager.PasswordSignInAsync(user, loginDto.Password, true, false);
        if (!result.Succeeded)
            return Unauthorized("Invalid username or password");

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

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new { message = "Logged out successfully" });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

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

    [HttpPost("create-user")]
    [Authorize]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto createUserDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Check if username already exists
        var existingUser = await _userManager.FindByNameAsync(createUserDto.Username);
        if (existingUser != null)
            return BadRequest("Username already exists");

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = createUserDto.Username,
            Name = createUserDto.Name,
            MustChangePassword = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, "1234");
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

        return CreatedAtAction(nameof(GetCurrentUser), new { id = user.Id }, userDto);
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var result = await _userManager.ChangePasswordAsync(user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        // Clear the must change password flag
        user.MustChangePassword = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Sign out and sign in again to refresh the cookie
        await _signInManager.SignOutAsync();
        await _signInManager.SignInAsync(user, true);

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
}

