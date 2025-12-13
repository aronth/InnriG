export const useProducts = () => {
    const config = useRuntimeConfig()
    const apiBase = config.public.apiBase
    const { apiFetch } = useApi()

    const getAllProducts = async (supplierId?: string) => {
        const url = supplierId
            ? `${apiBase}/api/products?supplierId=${supplierId}`
            : `${apiBase}/api/products`
        return await apiFetch(url)
    }

    const getProduct = async (id: string) => {
        return await apiFetch(`${apiBase}/api/products/${id}`)
    }

    const getProductHistory = async (id: string) => {
        return await apiFetch(`${apiBase}/api/products/${id}/history`)
    }

    const compareProductPrices = async (productCode: string) => {
        return await apiFetch(`${apiBase}/api/products/compare?productCode=${productCode}`)
    }

    return {
        getAllProducts,
        getProduct,
        getProductHistory,
        compareProductPrices
    }
}
