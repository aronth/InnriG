export interface MenuDto {
  id: string
  name: string
  forWho: string
  description?: string
  isActive: boolean
  createdAt: string
  updatedAt: string
  menuItems: MenuItemDto[]
}

export interface MenuItemDto {
  id: string
  menuId: string
  name: string
  description?: string
  price: number
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface CreateMenuDto {
  name: string
  forWho: string
  description?: string
  isActive: boolean
}

export interface UpdateMenuDto {
  name: string
  forWho: string
  description?: string
  isActive: boolean
}

export interface CreateMenuItemDto {
  menuId: string
  name: string
  description?: string
  price: number
  isActive: boolean
}

export interface UpdateMenuItemDto {
  name: string
  description?: string
  price: number
  isActive: boolean
}

