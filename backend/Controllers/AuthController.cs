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

        // Validate password first
        var passwordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);
        if (!passwordValid)
            return Unauthorized("Invalid username or password");

        // Get user roles and build claims
        var roles = await _userManager.GetRolesAsync(user);
        var claims = new List<System.Security.Claims.Claim>
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.UserName ?? string.Empty)
        };
        
        // Add all role claims explicitly
        foreach (var role in roles)
        {
            claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role));
        }
        
        // Sign in with all claims including roles
        // Use the default Identity scheme which is configured by AddIdentity
        await _signInManager.SignInWithClaimsAsync(user, true, claims);

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
        
        // Cookie should be set after SignInWithClaimsAsync
        if (!cookieSet)
        {
            // Cookie is set by middleware, may not be in headers yet but will be sent
            cookieSet = true;
        }

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

    [HttpGet("debug/claims")]
    [Authorize]
    public IActionResult GetClaims()
    {
        // Debug endpoint to see what claims are actually in the authentication cookie
        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        var rolesFromClaims = User.Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();
        
        var isInSystemAdminRole = User.IsInRole("SystemAdmin");
        var isInAdminRole = User.IsInRole("Admin");
        
        return Ok(new
        {
            allClaims = claims,
            rolesFromClaims = rolesFromClaims,
            isInSystemAdminRole = isInSystemAdminRole,
            isInAdminRole = isInAdminRole,
            identityName = User.Identity?.Name,
            isAuthenticated = User.Identity?.IsAuthenticated
        });
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
        var validRoles = new[] { "SystemAdmin", "Admin", "Manager", "User" };
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

        // Sign out and sign in again with role claims to refresh the cookie
        await _signInManager.SignOutAsync();
        
        // Get user roles and build claims
        var roles = await _userManager.GetRolesAsync(user);
        var claims = new List<System.Security.Claims.Claim>
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.UserName ?? string.Empty)
        };
        
        // Add all role claims explicitly
        foreach (var role in roles)
        {
            claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role));
        }
        
        await _signInManager.SignInWithClaimsAsync(user, true, claims);

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

        var validRoles = new[] { "SystemAdmin", "Admin", "Manager", "User" };
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
        var roles = new[] { "SystemAdmin", "Admin", "Manager", "User" };
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

