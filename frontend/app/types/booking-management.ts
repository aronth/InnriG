import type { CustomerDto } from './customer'

export interface BookingManagementDto {
  id: string
  customerId: string
  customer?: CustomerDto
  locationId?: string
  locationName?: string
  bookingDate: string
  startTime: string
  endTime?: string
  adultCount: number
  childCount: number
  status: string
  specialRequests?: string
  notes?: string
  needsPrint: boolean
  menuItems: BookingMenuItemDto[]
  createdAt: string
  updatedAt: string
}

export interface BookingMenuItemDto {
  id: string
  menuItemId: string
  menuItemName: string
  quantity: number
  unitPrice: number
  notes?: string
}

export interface CreateBookingDto {
  customerId: string
  locationId?: string
  bookingDate: string
  startTime: string
  endTime?: string
  adultCount: number
  childCount: number
  status: string
  specialRequests?: string
  notes?: string
  needsPrint: boolean
  menuItems: CreateBookingMenuItemDto[]
}

export interface CreateBookingMenuItemDto {
  menuItemId: string
  quantity: number
  unitPrice?: number
  notes?: string
}

export interface UpdateBookingDto {
  customerId: string
  locationId?: string
  bookingDate: string
  startTime: string
  endTime?: string
  adultCount: number
  childCount: number
  status: string
  specialRequests?: string
  notes?: string
  needsPrint: boolean
  menuItems: CreateBookingMenuItemDto[]
}

