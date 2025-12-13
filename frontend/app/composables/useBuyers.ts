export const useBuyers = () => {
    const config = useRuntimeConfig()
    const apiBase = config.public.apiBase
    const { apiFetch } = useApi()

    const getAllBuyers = async () => {
        return await apiFetch(`${apiBase}/api/buyers`)
    }

    const getBuyer = async (id: string) => {
        return await apiFetch(`${apiBase}/api/buyers/${id}`)
    }

    return {
        getAllBuyers,
        getBuyer
    }
}

