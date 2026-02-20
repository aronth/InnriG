using InnriGreifi.API.Models;
using InnriGreifi.API.Models.DTOs;

namespace InnriGreifi.API.Services;

public interface IMenuService
{
    Task<List<MenuDto>> GetAllMenusAsync();
    Task<MenuDto?> GetMenuByIdAsync(Guid id);
    Task<MenuDto> CreateMenuAsync(CreateMenuDto dto);
    Task<MenuDto?> UpdateMenuAsync(Guid id, UpdateMenuDto dto);
    Task<bool> DeleteMenuAsync(Guid id);
    Task<List<MenuItemDto>> GetMenuItemsByMenuIdAsync(Guid menuId);
    Task<MenuItemDto> CreateMenuItemAsync(CreateMenuItemDto dto);
    Task<MenuItemDto?> UpdateMenuItemAsync(Guid id, UpdateMenuItemDto dto);
    Task<bool> DeleteMenuItemAsync(Guid id);
}


