using InnriGreifi.API.Data;
using InnriGreifi.API.Models;
using InnriGreifi.API.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace InnriGreifi.API.Services;

public class MenuService : IMenuService
{
    private readonly AppDbContext _context;

    public MenuService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<MenuDto>> GetAllMenusAsync()
    {
        var menus = await _context.Menus
            .Include(m => m.MenuItems)
            .OrderBy(m => m.Name)
            .ToListAsync();

        return menus.Select(MapToDto).ToList();
    }

    public async Task<MenuDto?> GetMenuByIdAsync(Guid id)
    {
        var menu = await _context.Menus
            .Include(m => m.MenuItems)
            .FirstOrDefaultAsync(m => m.Id == id);

        return menu == null ? null : MapToDto(menu);
    }

    public async Task<MenuDto> CreateMenuAsync(CreateMenuDto dto)
    {
        var menu = new Menu
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            ForWho = dto.ForWho,
            Description = dto.Description,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Menus.Add(menu);
        await _context.SaveChangesAsync();

        return MapToDto(menu);
    }

    public async Task<MenuDto?> UpdateMenuAsync(Guid id, UpdateMenuDto dto)
    {
        var menu = await _context.Menus
            .Include(m => m.MenuItems)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (menu == null)
            return null;

        menu.Name = dto.Name;
        menu.ForWho = dto.ForWho;
        menu.Description = dto.Description;
        menu.IsActive = dto.IsActive;
        menu.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(menu);
    }

    public async Task<bool> DeleteMenuAsync(Guid id)
    {
        var menu = await _context.Menus
            .Include(m => m.MenuItems)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (menu == null)
            return false;

        // Soft delete - check if menu has items that are referenced by bookings
        // For now, we'll do a soft delete by setting IsActive to false
        // Hard delete can be added later if needed
        if (menu.MenuItems.Any())
        {
            // Soft delete
            menu.IsActive = false;
            menu.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Hard delete if no items
            _context.Menus.Remove(menu);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<MenuItemDto>> GetMenuItemsByMenuIdAsync(Guid menuId)
    {
        var menuItems = await _context.MenuItems
            .Where(mi => mi.MenuId == menuId)
            .OrderBy(mi => mi.Name)
            .ToListAsync();

        return menuItems.Select(MapMenuItemToDto).ToList();
    }

    public async Task<MenuItemDto> CreateMenuItemAsync(CreateMenuItemDto dto)
    {
        // Verify menu exists
        var menu = await _context.Menus.FindAsync(dto.MenuId);
        if (menu == null)
            throw new ArgumentException("Menu not found", nameof(dto.MenuId));

        var menuItem = new MenuItem
        {
            Id = Guid.NewGuid(),
            MenuId = dto.MenuId,
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.MenuItems.Add(menuItem);
        await _context.SaveChangesAsync();

        return MapMenuItemToDto(menuItem);
    }

    public async Task<MenuItemDto?> UpdateMenuItemAsync(Guid id, UpdateMenuItemDto dto)
    {
        var menuItem = await _context.MenuItems.FindAsync(id);

        if (menuItem == null)
            return null;

        menuItem.Name = dto.Name;
        menuItem.Description = dto.Description;
        menuItem.Price = dto.Price;
        menuItem.IsActive = dto.IsActive;
        menuItem.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapMenuItemToDto(menuItem);
    }

    public async Task<bool> DeleteMenuItemAsync(Guid id)
    {
        var menuItem = await _context.MenuItems.FindAsync(id);

        if (menuItem == null)
            return false;

        // Check if menu item is referenced by bookings
        // For now, soft delete
        menuItem.IsActive = false;
        menuItem.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    private static MenuDto MapToDto(Menu menu)
    {
        return new MenuDto
        {
            Id = menu.Id,
            Name = menu.Name,
            ForWho = menu.ForWho,
            Description = menu.Description,
            IsActive = menu.IsActive,
            CreatedAt = menu.CreatedAt,
            UpdatedAt = menu.UpdatedAt,
            MenuItems = menu.MenuItems.Select(MapMenuItemToDto).ToList()
        };
    }

    private static MenuItemDto MapMenuItemToDto(MenuItem menuItem)
    {
        return new MenuItemDto
        {
            Id = menuItem.Id,
            MenuId = menuItem.MenuId,
            Name = menuItem.Name,
            Description = menuItem.Description,
            Price = menuItem.Price,
            IsActive = menuItem.IsActive,
            CreatedAt = menuItem.CreatedAt,
            UpdatedAt = menuItem.UpdatedAt
        };
    }
}


