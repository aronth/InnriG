import type { BookingManagementDto, CreateBookingDto, UpdateBookingDto } from '~/types/booking-management'

export const useBookingManagement = () => {
  const config = useRuntimeConfig()
  const apiBase = config.public.apiBase
  const { apiFetch } = useApi()

  const getBookings = async (
    fromDate?: Date,
    toDate?: Date,
    customerId?: string,
    locationId?: string,
    status?: string
  ): Promise<BookingManagementDto[]> => {
    const params = new URLSearchParams()
    if (fromDate) params.append('fromDate', fromDate.toISOString())
    if (toDate) params.append('toDate', toDate.toISOString())
    if (customerId) params.append('customerId', customerId)
    if (locationId) params.append('locationId', locationId)
    if (status) params.append('status', status)

    return await apiFetch<BookingManagementDto[]>(`${apiBase}/api/bookings/manage?${params.toString()}`)
  }

  const getBookingById = async (id: string): Promise<BookingManagementDto> => {
    return await apiFetch<BookingManagementDto>(`${apiBase}/api/bookings/manage/${id}`)
  }

  const createBooking = async (dto: CreateBookingDto): Promise<BookingManagementDto> => {
    return await apiFetch<BookingManagementDto>(`${apiBase}/api/bookings/manage`, {
      method: 'POST',
      body: dto
    })
  }

  const updateBooking = async (id: string, dto: UpdateBookingDto): Promise<BookingManagementDto> => {
    return await apiFetch<BookingManagementDto>(`${apiBase}/api/bookings/manage/${id}`, {
      method: 'PUT',
      body: dto
    })
  }

  const deleteBooking = async (id: string): Promise<void> => {
    await apiFetch(`${apiBase}/api/bookings/manage/${id}`, {
      method: 'DELETE'
    })
  }

  return {
    getBookings,
    getBookingById,
    createBooking,
    updateBooking,
    deleteBooking
  }
}

