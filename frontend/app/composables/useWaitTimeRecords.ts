export enum Restaurant {
    Greifinn = 0,
    Spretturinn = 1
}

export interface WaitTimeRecordDto {
    id: string
    restaurant: Restaurant
    restaurantName: string
    sottMinutes?: number
    sentMinutes?: number
    isClosed: boolean
    scrapedAt: string
}

export interface WaitTimeRecordsFilters {
    restaurant?: Restaurant
    from?: string
    to?: string
}

export const useWaitTimeRecords = () => {
    const config = useRuntimeConfig()
    const apiBase = config.public.apiBase
    const { apiFetch } = useApi()

    const getRecords = async (filters?: WaitTimeRecordsFilters): Promise<WaitTimeRecordDto[]> => {
        const params = new URLSearchParams()
        
        if (filters?.restaurant !== undefined) params.append('restaurant', filters.restaurant.toString())
        if (filters?.from) params.append('from', filters.from)
        if (filters?.to) params.append('to', filters.to)

        const queryString = params.toString()
        const url = `${apiBase}/api/WaitTimeRecords${queryString ? `?${queryString}` : ''}`
        return await apiFetch<WaitTimeRecordDto[]>(url)
    }

    const getLatestRecords = async (): Promise<WaitTimeRecordDto[]> => {
        return await apiFetch<WaitTimeRecordDto[]>(`${apiBase}/api/WaitTimeRecords/latest`)
    }

    return {
        getRecords,
        getLatestRecords
    }
}
