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

        // Check if cookie was set by looking for Set-Cookie header
        // ASP.NET Core Identity sets the cookie via the authentication middleware
        var cookieName = "InnriGreifi.Auth";
        var cookieSet = false;
        
        // Check response headers for Set-Cookie header
        if (Response.Headers.TryGetValue("Set-Cookie", out var setCookieValues))
        {
            var setCookieHeader = string.Join(", ", setCookieValues.ToArray());
            cookieSet = setCookieHeader.Contains(cookieName);
        }
        
        // If sign-in succeeded with isPersistent=true, cookie should be set
        // So we can also trust that cookieSet will be true if sign-in succeeded
        if (!cookieSet && result.Succeeded)
        {
            // Cookie is set by middleware, may not be in headers yet but will be sent
            cookieSet = true;
        }

        var roles = await _userManager.GetRolesAsync(user);
        var userDto = new UserDto
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            Name = user.Name,
            MustChangePassword = user.MustChangePassword,
            CreatedAt = user.CreatedAt,
            Roles = roles.ToList()
        };

        return Ok(new
        {
            user = userDto,
            cookieSet = cookieSet
        });
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

        var roles = await _userManager.GetRolesAsync(user);
        var userDto = new UserDto
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            Name = user.Name,
            MustChangePassword = user.MustChangePassword,
            CreatedAt = user.CreatedAt,
            Roles = roles.ToList()
        };

        return Ok(userDto);
    }

    [HttpPost("create-user")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto createUserDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Check if username already exists
        var existingUser = await _userManager.FindByNameAsync(createUserDto.Username);
        if (existingUser != null)
            return BadRequest("Username already exists");

        // Validate role if provided
        var roleName = string.IsNullOrEmpty(createUserDto.Role) ? "User" : createUserDto.Role;
        var validRoles = new[] { "Admin", "Manager", "User" };
        if (!validRoles.Contains(roleName))
            return BadRequest($"Invalid role. Valid roles are: {string.Join(", ", validRoles)}");

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

        // Assign role (default to "User")
        var roleResult = await _userManager.AddToRoleAsync(user, roleName);
        if (!roleResult.Succeeded)
            return BadRequest(roleResult.Errors.Select(e => e.Description));

        var roles = await _userManager.GetRolesAsync(user);
        var userDto = new UserDto
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            Name = user.Name,
            MustChangePassword = user.MustChangePassword,
            CreatedAt = user.CreatedAt,
            Roles = roles.ToList()
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

        var roles = await _userManager.GetRolesAsync(user);
        var userDto = new UserDto
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            Name = user.Name,
            MustChangePassword = user.MustChangePassword,
            CreatedAt = user.CreatedAt,
            Roles = roles.ToList()
        };

        return Ok(userDto);
    }

    [HttpPost("assign-role")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleDto assignRoleDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.FindByIdAsync(assignRoleDto.UserId.ToString());
        if (user == null)
            return NotFound("User not found");

        var validRoles = new[] { "Admin", "Manager", "User" };
        if (!validRoles.Contains(assignRoleDto.Role))
            return BadRequest($"Invalid role. Valid roles are: {string.Join(", ", validRoles)}");

        if (await _userManager.IsInRoleAsync(user, assignRoleDto.Role))
            return BadRequest($"User already has the {assignRoleDto.Role} role");

        var result = await _userManager.AddToRoleAsync(user, assignRoleDto.Role);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        var roles = await _userManager.GetRolesAsync(user);
        var userDto = new UserDto
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            Name = user.Name,
            MustChangePassword = user.MustChangePassword,
            CreatedAt = user.CreatedAt,
            Roles = roles.ToList()
        };

        return Ok(userDto);
    }

    [HttpPost("remove-role")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveRole([FromBody] RemoveRoleDto removeRoleDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.FindByIdAsync(removeRoleDto.UserId.ToString());
        if (user == null)
            return NotFound("User not found");

        if (!await _userManager.IsInRoleAsync(user, removeRoleDto.Role))
            return BadRequest($"User does not have the {removeRoleDto.Role} role");

        var result = await _userManager.RemoveFromRoleAsync(user, removeRoleDto.Role);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        var roles = await _userManager.GetRolesAsync(user);
        var userDto = new UserDto
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            Name = user.Name,
            MustChangePassword = user.MustChangePassword,
            CreatedAt = user.CreatedAt,
            Roles = roles.ToList()
        };

        return Ok(userDto);
    }

    [HttpGet("roles")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetAvailableRoles()
    {
        var roles = new[] { "Admin", "Manager", "User" };
        return Ok(roles);
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto updateProfileDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        // Update name only (username is immutable)
        user.Name = updateProfileDto.Name;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        var roles = await _userManager.GetRolesAsync(user);
        var userDto = new UserDto
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            Name = user.Name,
            MustChangePassword = user.MustChangePassword,
            CreatedAt = user.CreatedAt,
            Roles = roles.ToList()
        };

        return Ok(userDto);
    }
}

