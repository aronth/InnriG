using InnriGreifi.API.Models.DTOs;
using InnriGreifi.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InnriGreifi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "User,Manager,Admin")]
public class MenusController : ControllerBase
{
    private readonly IMenuService _menuService;
    private readonly ILogger<MenusController> _logger;

    public MenusController(IMenuService menuService, ILogger<MenusController> logger)
    {
        _menuService = menuService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllMenus()
    {
        try
        {
            var menus = await _menuService.GetAllMenusAsync();
            return Ok(menus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching menus");
            return StatusCode(500, new { error = "Error fetching menus", details = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetMenu(Guid id)
    {
        try
        {
            var menu = await _menuService.GetMenuByIdAsync(id);
            if (menu == null)
                return NotFound();

            return Ok(menu);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching menu {MenuId}", id);
            return StatusCode(500, new { error = "Error fetching menu", details = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> CreateMenu([FromBody] CreateMenuDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var menu = await _menuService.CreateMenuAsync(dto);
            return CreatedAtAction(nameof(GetMenu), new { id = menu.Id }, menu);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating menu");
            return StatusCode(500, new { error = "Error creating menu", details = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> UpdateMenu(Guid id, [FromBody] UpdateMenuDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var menu = await _menuService.UpdateMenuAsync(id, dto);
            if (menu == null)
                return NotFound();

            return Ok(menu);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating menu {MenuId}", id);
            return StatusCode(500, new { error = "Error updating menu", details = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> DeleteMenu(Guid id)
    {
        try
        {
            var deleted = await _menuService.DeleteMenuAsync(id);
            if (!deleted)
                return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting menu {MenuId}", id);
            return StatusCode(500, new { error = "Error deleting menu", details = ex.Message });
        }
    }

    [HttpGet("{id}/items")]
    public async Task<IActionResult> GetMenuItems(Guid id)
    {
        try
        {
            var items = await _menuService.GetMenuItemsByMenuIdAsync(id);
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching menu items for menu {MenuId}", id);
            return StatusCode(500, new { error = "Error fetching menu items", details = ex.Message });
        }
    }

    [HttpPost("{id}/items")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> CreateMenuItem(Guid id, [FromBody] CreateMenuItemDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Ensure MenuId matches
            if (dto.MenuId != id)
                return BadRequest("MenuId mismatch");

            var menuItem = await _menuService.CreateMenuItemAsync(dto);
            return CreatedAtAction(nameof(GetMenuItem), new { itemId = menuItem.Id }, menuItem);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating menu item");
            return StatusCode(500, new { error = "Error creating menu item", details = ex.Message });
        }
    }

    [HttpGet("items/{itemId}")]
    public async Task<IActionResult> GetMenuItem(Guid itemId)
    {
        try
        {
            // Get menu item via menu service (we'll need to add this method or get via menu)
            // For now, return not implemented or add method to service
            return StatusCode(501, "Not implemented - use GET /api/menus/{id} to get menu with items");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching menu item {ItemId}", itemId);
            return StatusCode(500, new { error = "Error fetching menu item", details = ex.Message });
        }
    }

    [HttpPut("items/{itemId}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> UpdateMenuItem(Guid itemId, [FromBody] UpdateMenuItemDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var menuItem = await _menuService.UpdateMenuItemAsync(itemId, dto);
            if (menuItem == null)
                return NotFound();

            return Ok(menuItem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating menu item {ItemId}", itemId);
            return StatusCode(500, new { error = "Error updating menu item", details = ex.Message });
        }
    }

    [HttpDelete("items/{itemId}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> DeleteMenuItem(Guid itemId)
    {
        try
        {
            var deleted = await _menuService.DeleteMenuItemAsync(itemId);
            if (!deleted)
                return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting menu item {ItemId}", itemId);
            return StatusCode(500, new { error = "Error deleting menu item", details = ex.Message });
        }
    }
}


