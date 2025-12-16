export enum Restaurant {
    Greifinn = 0,
    Spretturinn = 1
}

export interface WaitTimeNotificationDto {
    id: string
    userId: string
    restaurant: Restaurant
    restaurantName: string
    sottThresholdMinutes?: number
    sentThresholdMinutes?: number
    isEnabled: boolean
    lastNotifiedSott?: string
    lastNotifiedSent?: string
    createdAt: string
    updatedAt: string
}

export interface CreateWaitTimeNotificationDto {
    restaurant: Restaurant
    pushoverUserKey: string
    sottThresholdMinutes?: number
    sentThresholdMinutes?: number
    isEnabled?: boolean
}

export interface UpdateWaitTimeNotificationDto {
    pushoverUserKey?: string
    sottThresholdMinutes?: number
    sentThresholdMinutes?: number
    isEnabled?: boolean
}

export const useWaitTimeNotifications = () => {
    const config = useRuntimeConfig()
    const apiBase = config.public.apiBase
    const { apiFetch } = useApi()

    const getNotifications = async (): Promise<WaitTimeNotificationDto[]> => {
        return await apiFetch<WaitTimeNotificationDto[]>(`${apiBase}/api/WaitTimeNotifications`)
    }

    const getNotification = async (id: string): Promise<WaitTimeNotificationDto> => {
        return await apiFetch<WaitTimeNotificationDto>(`${apiBase}/api/WaitTimeNotifications/${id}`)
    }

    const createNotification = async (dto: CreateWaitTimeNotificationDto): Promise<WaitTimeNotificationDto> => {
        return await apiFetch<WaitTimeNotificationDto>(`${apiBase}/api/WaitTimeNotifications`, {
            method: 'POST',
            body: dto
        })
    }

    const updateNotification = async (id: string, dto: UpdateWaitTimeNotificationDto): Promise<WaitTimeNotificationDto> => {
        return await apiFetch<WaitTimeNotificationDto>(`${apiBase}/api/wait-time-notifications/${id}`, {
            method: 'PUT',
            body: dto
        })
    }

    const deleteNotification = async (id: string): Promise<void> => {
        await apiFetch(`${apiBase}/api/wait-time-notifications/${id}`, {
            method: 'DELETE'
        })
    }

    return {
        getNotifications,
        getNotification,
        createNotification,
        updateNotification,
        deleteNotification
    }
}
