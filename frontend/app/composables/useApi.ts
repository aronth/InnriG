export const useApi = () => {
    const config = useRuntimeConfig()
    const apiBase = config.public.apiBase

    const apiFetch = async <T = any>(url: string, options: any = {}): Promise<T> => {
        const fullUrl = url.startsWith('http') ? url : `${apiBase}${url}`
        
        try {
            const response = await $fetch<T>(fullUrl, {
                ...options,
                credentials: 'include', // Always include cookies
            })
            return response
        } catch (error: any) {
            // Handle 401 unauthorized - redirect to login
            if (error.status === 401 || error.statusCode === 401) {
                // Only redirect if we're not already on login page
                if (typeof window !== 'undefined' && !window.location.pathname.includes('/login')) {
                    navigateTo('/login')
                }
            }
            throw error
        }
    }

    return {
        apiFetch
    }
}

