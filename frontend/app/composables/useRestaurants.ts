import type { Restaurant } from '~/types/giftCard'

export const useRestaurants = () => {
    const config = useRuntimeConfig()
    const apiBase = config.public.apiBase
    const { apiFetch } = useApi()
    
    const getRestaurants = async (): Promise<Restaurant[]> => {
        return await apiFetch<Restaurant[]>(`${apiBase}/api/restaurants`)
    }
    
    const getRestaurant = async (id: string): Promise<Restaurant> => {
        return await apiFetch<Restaurant>(`${apiBase}/api/restaurants/${id}`)
    }
    
    return {
        getRestaurants,
        getRestaurant
    }
}

