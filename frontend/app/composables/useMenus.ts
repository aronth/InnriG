import type { MenuDto, MenuItemDto, CreateMenuDto, UpdateMenuDto, CreateMenuItemDto, UpdateMenuItemDto } from '~/types/menu'

export const useMenus = () => {
  const config = useRuntimeConfig()
  const apiBase = config.public.apiBase
  const { apiFetch } = useApi()

  const getAllMenus = async (): Promise<MenuDto[]> => {
    return await apiFetch<MenuDto[]>(`${apiBase}/api/menus`)
  }

  const getMenuById = async (id: string): Promise<MenuDto> => {
    return await apiFetch<MenuDto>(`${apiBase}/api/menus/${id}`)
  }

  const createMenu = async (dto: CreateMenuDto): Promise<MenuDto> => {
    return await apiFetch<MenuDto>(`${apiBase}/api/menus`, {
      method: 'POST',
      body: dto
    })
  }

  const updateMenu = async (id: string, dto: UpdateMenuDto): Promise<MenuDto> => {
    return await apiFetch<MenuDto>(`${apiBase}/api/menus/${id}`, {
      method: 'PUT',
      body: dto
    })
  }

  const deleteMenu = async (id: string): Promise<void> => {
    await apiFetch(`${apiBase}/api/menus/${id}`, {
      method: 'DELETE'
    })
  }

  const getMenuItems = async (menuId: string): Promise<MenuItemDto[]> => {
    return await apiFetch<MenuItemDto[]>(`${apiBase}/api/menus/${menuId}/items`)
  }

  const createMenuItem = async (menuId: string, dto: CreateMenuItemDto): Promise<MenuItemDto> => {
    return await apiFetch<MenuItemDto>(`${apiBase}/api/menus/${menuId}/items`, {
      method: 'POST',
      body: dto
    })
  }

  const updateMenuItem = async (itemId: string, dto: UpdateMenuItemDto): Promise<MenuItemDto> => {
    return await apiFetch<MenuItemDto>(`${apiBase}/api/menus/items/${itemId}`, {
      method: 'PUT',
      body: dto
    })
  }

  const deleteMenuItem = async (itemId: string): Promise<void> => {
    await apiFetch(`${apiBase}/api/menus/items/${itemId}`, {
      method: 'DELETE'
    })
  }

  return {
    getAllMenus,
    getMenuById,
    createMenu,
    updateMenu,
    deleteMenu,
    getMenuItems,
    createMenuItem,
    updateMenuItem,
    deleteMenuItem
  }
}

