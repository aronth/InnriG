export interface OrderImportResultDto {
  batchId: string
  fileName: string
  importedAt: string
  totalRowsInSheet: number
  importedRows: number
  skippedRows: number
  warnings: string[]
}

export interface OrderImportBatch {
  id: string
  fileName: string
  importedAt: string
  rowCount: number
}

export interface OrderRow {
  id: string
  orderImportBatchId: string
  sourceRowNumber: number
  status?: string | null
  date?: string | null
  salesman?: string | null
  debtor?: string | null
  totalAmountWithVat?: number | null
  cashRegisterNumber?: number | null
  invoiceNumber?: number | null
  description?: string | null
  createdOnRegister?: boolean | null
  orderType?: string | null
  createdDate?: string | null
  address?: string | null
  orderDate?: string | null
  deliveryDate?: string | null
  deleted?: boolean | null
  invoiceText3Raw?: string | null
  scannedAt?: string | null
  checkedOutAt?: string | null
  orderNumber?: string | null
  deliveryMethod: string
  orderSource: string
  orderTime?: string | null
  readyTime?: string | null
  waitTimeMin?: number | null
  createdAt: string
  updatedAt: string
}

export interface OrderLateCountByMethodDto {
  deliveryMethod: string
  total: number
  evaluable: number
  lateCount: number
  lateRatio: number
}

export interface OrderLateRatioPointDto {
  periodStart: string
  total: number
  evaluable: number
  lateCount: number
  lateRatio: number
}

// New DTOs for reporting dashboard
export interface OrderReportSummaryDto {
  totalOrders: number
  evaluableOrders: number
  lateOrders: number
  lateRatio: number
  avgWaitTimeMin: number | null
  p90WaitTimeMin: number | null
  ordersByMethod: Record<string, number>
  revenueByMethod: Record<string, number>
  ordersBySource: Record<string, number>
  revenueBySource: Record<string, number>
  totalRevenue: number
  averageOrderValue: number
}

export interface OrderWaitTimeSeriesPointDto {
  periodStart: string
  count: number
  avgWaitTimeMin: number | null
  p90WaitTimeMin: number | null
}

export interface OrderWaitTimeBucketDto {
  minMinutes: number
  maxMinutes: number | null
  count: number
  label: string
}

export interface OrderWaitTimeDistributionDto {
  bucketSize: number
  buckets: OrderWaitTimeBucketDto[]
}

export interface OrderHeatmapCellDto {
  weekday: number
  hour: number
  value: number
  total?: number | null
  evaluable?: number | null
  lateCount?: number | null
}

export interface OrderHeatmapDto {
  dataType: string
  cells: OrderHeatmapCellDto[][]
  weekdayLabels: string[]
  hourLabels: string[]
}

export interface WeekdayExpectedIndexDto {
  weekday: number
  weekdayName: string
  daysInPeriod: number
  actualCount: number
  baselineAvgPerDay: number
  expectedCount: number
  index: number
}

export interface OrderVolumeExpectedIndexDto {
  periodStart: string
  periodEnd: string
  baselineWindowDays: number
  actualTotal: number
  expectedTotal: number
  index: number
  percentDiff: number
  byWeekday: WeekdayExpectedIndexDto[]
}

export interface OrderGrowthWeekDto {
  weekStart: string
  weekEnd: string
  totalOrders: number
  evaluableOrders: number
  lateOrders: number
  lateRatio: number
  avgWaitTimeMin?: number | null
  p90WaitTimeMin?: number | null
  totalOrdersGrowth?: number | null
  evaluableOrdersGrowth?: number | null
  lateOrdersGrowth?: number | null
  lateRatioGrowth?: number | null
  avgWaitTimeGrowth?: number | null
  p90WaitTimeGrowth?: number | null
}

export interface PeriodComparisonDto {
  currentPeriod: PeriodInfo
  previousPeriod: PeriodInfo
  comparison: ComparisonMetrics
}

export interface PeriodInfo {
  start: string
  end: string
  totalOrders: number
  totalRevenue: number
  averageOrderValue: number
  evaluableOrders: number
  lateOrders: number
  lateRatio: number
  avgWaitTimeMin?: number | null
  p90WaitTimeMin?: number | null
  ordersByMethod: Record<string, number>
  revenueByMethod: Record<string, number>
  ordersBySource: Record<string, number>
  revenueBySource: Record<string, number>
  byDayOfWeek: DayOfWeekMetrics[]
}

export interface DayOfWeekMetrics {
  weekday: number
  weekdayName: string
  orderCount: number
  revenue: number
  lateOrders: number
  lateRatio: number
  avgWaitTimeMin?: number | null
}

export interface ComparisonMetrics {
  ordersChangePercent: number
  revenueChangePercent: number
  lateRatioChangePercent: number
  waitTimeChangePercent?: number | null
  p90WaitTimeChangePercent?: number | null
  ordersByMethodChangePercent: Record<string, number>
  revenueByMethodChangePercent: Record<string, number>
}

export interface ForecastDto {
  forecastPeriodStart: string
  forecastPeriodEnd: string
  predictedOrders: number
  predictedOrdersMin: number
  predictedOrdersMax: number
  predictedRevenue: number
  predictedRevenueMin: number
  predictedRevenueMax: number
  predictedLateRatio: number
  predictedAvgWaitTime?: number | null
  predictedP90WaitTime?: number | null
  trendDirection: string
  trendStrength: number
  baselineWindowDays: number
  byWeekday: ForecastWeekdayDto[]
}

export interface ForecastWeekdayDto {
  weekday: number
  weekdayName: string
  daysInPeriod: number
  predictedOrders: number
  predictedRevenue: number
}

export interface OrderTimelineReportDto {
  date: string
  orders: OrderTimelinePointDto[]
}

export interface OrderTimelinePointDto {
  orderId: string
  orderTime: string
  deliveryMethod: string
  restaurantId?: string | null
  waitTimeMinutes?: number | null
  timeToScanMinutes?: number | null
  timeToCheckoutMinutes?: number | null
  orderNumber?: string | null
  totalAmount?: number | null
}

export interface PaginatedOrderRows {
  items: OrderRow[]
  totalCount: number
  skip: number
  take: number
  page: number
  totalPages: number
}

export const useOrders = () => {
  const config = useRuntimeConfig()
  const apiBase = config.public.apiBase
  const { apiFetch } = useApi()

  const uploadOrders = async (file: File, restaurantId?: string | null): Promise<OrderImportResultDto> => {
    const formData = new FormData()
    formData.append('file', file)

    const url = new URL(`${apiBase}/api/orders/upload`)
    if (restaurantId) {
      url.searchParams.set('restaurantId', restaurantId)
    }

    return await apiFetch<OrderImportResultDto>(url.toString(), {
      method: 'POST',
      body: formData
    })
  }

  const getBatches = async (): Promise<OrderImportBatch[]> => {
    return await apiFetch<OrderImportBatch[]>(`${apiBase}/api/orders/batches`)
  }

  const deleteBatch = async (id: string): Promise<void> => {
    await apiFetch(`${apiBase}/api/orders/batches/${id}`, {
      method: 'DELETE'
    })
  }

  const getRows = async (params?: {
    batchId?: string
    restaurantId?: string
    deliveryMethod?: string
    fromDate?: string
    toDate?: string
    isLate?: boolean
    skip?: number
    take?: number
  }): Promise<OrderRow[]> => {
    const sp = new URLSearchParams()
    if (params?.batchId) sp.set('batchId', params.batchId)
    if (params?.restaurantId) sp.set('restaurantId', params.restaurantId)
    if (params?.deliveryMethod) sp.set('deliveryMethod', params.deliveryMethod)
    if (params?.fromDate) sp.set('fromDate', params.fromDate)
    if (params?.toDate) sp.set('toDate', params.toDate)
    if (typeof params?.isLate === 'boolean') sp.set('isLate', String(params.isLate))
    if (typeof params?.skip === 'number') sp.set('skip', String(params.skip))
    if (typeof params?.take === 'number') sp.set('take', String(params.take))

    const url = `${apiBase}/api/orders/rows${sp.toString() ? `?${sp.toString()}` : ''}`
    return await apiFetch<OrderRow[]>(url)
  }

  const getRowsPaginated = async (params?: {
    restaurantId?: string
    deliveryMethod?: string
    fromDate?: string
    toDate?: string
    isLate?: boolean
    skip?: number
    take?: number
  }): Promise<PaginatedOrderRows> => {
    const sp = new URLSearchParams()
    sp.set('includeTotal', 'true')
    if (params?.restaurantId) sp.set('restaurantId', params.restaurantId)
    if (params?.deliveryMethod) sp.set('deliveryMethod', params.deliveryMethod)
    if (params?.fromDate) sp.set('fromDate', params.fromDate)
    if (params?.toDate) sp.set('toDate', params.toDate)
    if (typeof params?.isLate === 'boolean') sp.set('isLate', String(params.isLate))
    if (typeof params?.skip === 'number') sp.set('skip', String(params.skip))
    if (typeof params?.take === 'number') sp.set('take', String(params.take))

    const url = `${apiBase}/api/orders/rows?${sp.toString()}`
    const result = await apiFetch<{ items: OrderRow[], totalCount: number, skip: number, take: number, page: number, totalPages: number }>(url)
    return {
      items: result.items,
      totalCount: result.totalCount,
      skip: result.skip,
      take: result.take,
      page: result.page,
      totalPages: result.totalPages
    }
  }

  const updateReadyTime = async (orderId: string, readyTime: string | null): Promise<OrderRow> => {
    const url = `${apiBase}/api/orders/rows/${orderId}/ready-time`
    return await apiFetch<OrderRow>(url, {
      method: 'PUT',
      body: JSON.stringify({ readyTime })
    })
  }

  // Legacy endpoints
  const getLateCountByMethod = async (params?: { from?: string; to?: string }): Promise<OrderLateCountByMethodDto[]> => {
    const sp = new URLSearchParams()
    if (params?.from) sp.set('from', params.from)
    if (params?.to) sp.set('to', params.to)
    const url = `${apiBase}/api/orders/reports/late-count-by-method${sp.toString() ? `?${sp.toString()}` : ''}`
    return await apiFetch<OrderLateCountByMethodDto[]>(url)
  }

  const getLateRatio = async (params: {
    deliveryMethod: string
    granularity: 'day' | 'week' | 'month'
    from?: string
    to?: string
  }): Promise<OrderLateRatioPointDto[]> => {
    const sp = new URLSearchParams()
    sp.set('deliveryMethod', params.deliveryMethod)
    sp.set('granularity', params.granularity)
    if (params.from) sp.set('from', params.from)
    if (params.to) sp.set('to', params.to)
    return await apiFetch<OrderLateRatioPointDto[]>(`${apiBase}/api/orders/reports/late-ratio?${sp.toString()}`)
  }

  // New reporting endpoints
  const getSummary = async (params?: {
    from?: string
    to?: string
    deliveryMethod?: string
    restaurantId?: string
  }): Promise<OrderReportSummaryDto> => {
    const sp = new URLSearchParams()
    if (params?.from) sp.set('from', params.from)
    if (params?.to) sp.set('to', params.to)
    if (params?.deliveryMethod) sp.set('deliveryMethod', params.deliveryMethod)
    if (params?.restaurantId) sp.set('restaurantId', params.restaurantId)
    const url = `${apiBase}/api/orders/reports/summary${sp.toString() ? `?${sp.toString()}` : ''}`
    return await apiFetch<OrderReportSummaryDto>(url)
  }

  const getCompleteSummary = async (): Promise<OrderReportSummaryDto> => {
    const url = `${apiBase}/api/orders/reports/summary/complete`
    return await apiFetch<OrderReportSummaryDto>(url)
  }

  const getWaitTimeSeries = async (params?: {
    from?: string
    to?: string
    granularity?: 'day' | 'week' | 'month'
    deliveryMethod?: string
    restaurantId?: string
  }): Promise<OrderWaitTimeSeriesPointDto[]> => {
    const sp = new URLSearchParams()
    if (params?.from) sp.set('from', params.from)
    if (params?.to) sp.set('to', params.to)
    if (params?.granularity) sp.set('granularity', params.granularity)
    if (params?.deliveryMethod) sp.set('deliveryMethod', params.deliveryMethod)
    if (params?.restaurantId) sp.set('restaurantId', params.restaurantId)
    const url = `${apiBase}/api/orders/reports/waittime-series${sp.toString() ? `?${sp.toString()}` : ''}`
    return await apiFetch<OrderWaitTimeSeriesPointDto[]>(url)
  }

  const getWaitTimeDistribution = async (params?: {
    from?: string
    to?: string
    deliveryMethod?: string
    bucketSize?: number
    restaurantId?: string
  }): Promise<OrderWaitTimeDistributionDto> => {
    const sp = new URLSearchParams()
    if (params?.from) sp.set('from', params.from)
    if (params?.to) sp.set('to', params.to)
    if (params?.deliveryMethod) sp.set('deliveryMethod', params.deliveryMethod)
    if (typeof params?.bucketSize === 'number') sp.set('bucketSize', String(params.bucketSize))
    if (params?.restaurantId) sp.set('restaurantId', params.restaurantId)
    const url = `${apiBase}/api/orders/reports/waittime-distribution${sp.toString() ? `?${sp.toString()}` : ''}`
    return await apiFetch<OrderWaitTimeDistributionDto>(url)
  }

  const getLateSeries = async (params?: {
    from?: string
    to?: string
    granularity?: 'day' | 'week' | 'month'
    deliveryMethod?: string
    restaurantId?: string
  }): Promise<OrderLateRatioPointDto[]> => {
    const sp = new URLSearchParams()
    if (params?.from) sp.set('from', params.from)
    if (params?.to) sp.set('to', params.to)
    if (params?.granularity) sp.set('granularity', params.granularity)
    if (params?.deliveryMethod) sp.set('deliveryMethod', params.deliveryMethod)
    if (params?.restaurantId) sp.set('restaurantId', params.restaurantId)
    const url = `${apiBase}/api/orders/reports/late-series${sp.toString() ? `?${sp.toString()}` : ''}`
    return await apiFetch<OrderLateRatioPointDto[]>(url)
  }

  const getVolumeHeatmap = async (params?: {
    from?: string
    to?: string
    deliveryMethod?: string
    restaurantId?: string
  }): Promise<OrderHeatmapDto> => {
    const sp = new URLSearchParams()
    if (params?.from) sp.set('from', params.from)
    if (params?.to) sp.set('to', params.to)
    if (params?.deliveryMethod) sp.set('deliveryMethod', params.deliveryMethod)
    if (params?.restaurantId) sp.set('restaurantId', params.restaurantId)
    const url = `${apiBase}/api/orders/reports/heatmap/volume${sp.toString() ? `?${sp.toString()}` : ''}`
    return await apiFetch<OrderHeatmapDto>(url)
  }

  const getLateHeatmap = async (params?: {
    from?: string
    to?: string
    deliveryMethod?: string
    restaurantId?: string
  }): Promise<OrderHeatmapDto> => {
    const sp = new URLSearchParams()
    if (params?.from) sp.set('from', params.from)
    if (params?.to) sp.set('to', params.to)
    if (params?.deliveryMethod) sp.set('deliveryMethod', params.deliveryMethod)
    if (params?.restaurantId) sp.set('restaurantId', params.restaurantId)
    const url = `${apiBase}/api/orders/reports/heatmap/late${sp.toString() ? `?${sp.toString()}` : ''}`
    return await apiFetch<OrderHeatmapDto>(url)
  }

  const getWaitTimeHeatmap = async (params?: {
    from?: string
    to?: string
    deliveryMethod?: string
    restaurantId?: string
  }): Promise<OrderHeatmapDto> => {
    const sp = new URLSearchParams()
    if (params?.from) sp.set('from', params.from)
    if (params?.to) sp.set('to', params.to)
    if (params?.deliveryMethod) sp.set('deliveryMethod', params.deliveryMethod)
    if (params?.restaurantId) sp.set('restaurantId', params.restaurantId)
    const url = `${apiBase}/api/orders/reports/heatmap/waittime${sp.toString() ? `?${sp.toString()}` : ''}`
    return await apiFetch<OrderHeatmapDto>(url)
  }

  const getP90WaitTimeHeatmap = async (params?: {
    from?: string
    to?: string
    deliveryMethod?: string
    restaurantId?: string
  }): Promise<OrderHeatmapDto> => {
    const sp = new URLSearchParams()
    if (params?.from) sp.set('from', params.from)
    if (params?.to) sp.set('to', params.to)
    if (params?.deliveryMethod) sp.set('deliveryMethod', params.deliveryMethod)
    if (params?.restaurantId) sp.set('restaurantId', params.restaurantId)
    const url = `${apiBase}/api/orders/reports/heatmap/p90-waittime${sp.toString() ? `?${sp.toString()}` : ''}`
    return await apiFetch<OrderHeatmapDto>(url)
  }

  const getVolumeExpectedIndex = async (params: {
    from: string
    to: string
    baselineWindowDays?: number
    deliveryMethod?: string
    restaurantId?: string
  }): Promise<OrderVolumeExpectedIndexDto> => {
    const sp = new URLSearchParams()
    sp.set('from', params.from)
    sp.set('to', params.to)
    if (typeof params.baselineWindowDays === 'number') sp.set('baselineWindowDays', String(params.baselineWindowDays))
    if (params.deliveryMethod) sp.set('deliveryMethod', params.deliveryMethod)
    if (params.restaurantId) sp.set('restaurantId', params.restaurantId)
    const url = `${apiBase}/api/orders/reports/volume-expected-index?${sp.toString()}`
    return await apiFetch<OrderVolumeExpectedIndexDto>(url)
  }

  const getGrowthByWeek = async (params: {
    from: string
    to: string
    deliveryMethod?: string
    weekday?: number
    isLate?: boolean
    restaurantId?: string
  }): Promise<OrderGrowthWeekDto[]> => {
    const sp = new URLSearchParams()
    sp.set('from', params.from)
    sp.set('to', params.to)
    if (params.deliveryMethod) sp.set('deliveryMethod', params.deliveryMethod)
    if (typeof params.weekday === 'number') sp.set('weekday', String(params.weekday))
    if (typeof params.isLate === 'boolean') sp.set('isLate', String(params.isLate))
    if (params.restaurantId) sp.set('restaurantId', params.restaurantId)
    const url = `${apiBase}/api/orders/reports/growth-by-week?${sp.toString()}`
    return await apiFetch<OrderGrowthWeekDto[]>(url)
  }

  const getPeriodComparison = async (params: {
    periodType: 'week' | 'month'
    periodStart: string
    deliveryMethod?: string
    restaurantId?: string
  }): Promise<PeriodComparisonDto> => {
    const sp = new URLSearchParams()
    sp.set('periodType', params.periodType)
    sp.set('periodStart', params.periodStart)
    if (params.deliveryMethod) sp.set('deliveryMethod', params.deliveryMethod)
    if (params.restaurantId) sp.set('restaurantId', params.restaurantId)
    const url = `${apiBase}/api/orders/reports/period-comparison?${sp.toString()}`
    return await apiFetch<PeriodComparisonDto>(url)
  }

  const getForecast = async (params: {
    periodType: 'week' | 'month'
    targetPeriodStart: string
    deliveryMethod?: string
    restaurantId?: string
    lookbackDays?: number
  }): Promise<ForecastDto> => {
    const sp = new URLSearchParams()
    sp.set('periodType', params.periodType)
    sp.set('targetPeriodStart', params.targetPeriodStart)
    if (params.deliveryMethod) sp.set('deliveryMethod', params.deliveryMethod)
    if (params.restaurantId) sp.set('restaurantId', params.restaurantId)
    if (typeof params.lookbackDays === 'number') sp.set('lookbackDays', String(params.lookbackDays))
    const url = `${apiBase}/api/orders/reports/forecast?${sp.toString()}`
    return await apiFetch<ForecastDto>(url)
  }

  const getTimeline = async (params: {
    date: string
    deliveryMethod?: string
    restaurantId?: string
  }): Promise<OrderTimelineReportDto> => {
    const sp = new URLSearchParams()
    sp.set('date', params.date)
    if (params.deliveryMethod) sp.set('deliveryMethod', params.deliveryMethod)
    if (params.restaurantId) sp.set('restaurantId', params.restaurantId)
    const url = `${apiBase}/api/orders/reports/timeline?${sp.toString()}`
    return await apiFetch<OrderTimelineReportDto>(url)
  }

  return {
    uploadOrders,
    getBatches,
    deleteBatch,
    getRows,
    getRowsPaginated,
    updateReadyTime,
    getLateCountByMethod,
    getLateRatio,
    // New reporting functions
    getSummary,
    getCompleteSummary,
    getWaitTimeSeries,
    getWaitTimeDistribution,
    getLateSeries,
    getVolumeHeatmap,
    getLateHeatmap,
    getWaitTimeHeatmap,
    getP90WaitTimeHeatmap,
    getVolumeExpectedIndex,
    getGrowthByWeek,
    getPeriodComparison,
    getForecast,
    getTimeline
  }
}

export interface RemainingTimeIntervalData {
  timeLabel: string
  intervalStart: string
  averageRemainingMinutes: number | null
  orderCount: number
}

export interface RemainingTimeAggregationDto {
  date: string
  dateLabel: string
  intervals: RemainingTimeIntervalData[]
}

export const useRemainingTime = () => {
  const config = useRuntimeConfig()
  const apiBase = config.public.apiBase
  const { apiFetch } = useApi()

  const getRemainingTimeAggregations = async (params?: {
    restaurantId?: string
    deliveryMethod?: string
    fromDate?: string
    toDate?: string
  }): Promise<RemainingTimeAggregationDto[]> => {
    const sp = new URLSearchParams()
    if (params?.restaurantId) sp.set('restaurantId', params.restaurantId)
    if (params?.deliveryMethod) sp.set('deliveryMethod', params.deliveryMethod)
    if (params?.fromDate) sp.set('fromDate', params.fromDate)
    if (params?.toDate) sp.set('toDate', params.toDate)
    const url = `${apiBase}/api/orders/remaining-time-aggregations${sp.toString() ? `?${sp.toString()}` : ''}`
    return await apiFetch<RemainingTimeAggregationDto[]>(url)
  }

  return {
    getRemainingTimeAggregations
  }
}