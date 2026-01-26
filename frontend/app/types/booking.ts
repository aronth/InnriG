export interface BookingDto {
  date: string
  startTime: string
  endTime?: string
  customerName: string
  shortDescription: string
  adultCount: number
  childCount: number
  locationCode?: string
  status: string
  detailUrl: string
  needsPrint: boolean
}

export interface BookingDayDto {
  date: string
  dayName: string
  bookings: BookingDto[]
}

export interface BookingWeekDto {
  weekStart: string
  weekEnd: string
  days: BookingDayDto[]
}

