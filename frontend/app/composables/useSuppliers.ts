export const useSuppliers = () => {
    const config = useRuntimeConfig()
    const apiBase = config.public.apiBase
    const { apiFetch } = useApi()

    const getAllSuppliers = async () => {
        return await apiFetch(`${apiBase}/api/suppliers`)
    }

    const getSupplier = async (id: string) => {
        return await apiFetch(`${apiBase}/api/suppliers/${id}`)
    }

    return {
        getAllSuppliers,
        getSupplier
    }
}
