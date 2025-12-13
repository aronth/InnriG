export interface ProductListDto {
    id: string
    productCode: string
    name: string
    description?: string
    currentUnit?: string
    supplierId: string
    supplierName: string
    latestPrice?: number
    listPrice?: number
    discount?: number
    discountPercentage?: number
    lastPurchaseDate?: string
}

export interface ProductLookupDto {
    id: string
    productCode: string
    name: string
    supplierName: string
    latestPrice?: number
}

export interface PaginatedResponse<T> {
    items: T[]
    totalCount: number
    page: number
    pageSize: number
    totalPages: number
    hasPreviousPage: boolean
    hasNextPage: boolean
}

export interface ProductFilters {
    supplierId?: string
    search?: string
    minPrice?: number
    maxPrice?: number
    hasPrice?: boolean
    sortBy?: string
    sortOrder?: 'asc' | 'desc'
    page?: number
    pageSize?: number
}

export const useProducts = () => {
    const config = useRuntimeConfig()
    const apiBase = config.public.apiBase
    const { apiFetch } = useApi()

    const getAllProducts = async (filters?: ProductFilters): Promise<PaginatedResponse<ProductListDto>> => {
        const params = new URLSearchParams()
        
        if (filters?.supplierId) params.append('supplierId', filters.supplierId)
        if (filters?.search) params.append('search', filters.search)
        if (filters?.minPrice !== undefined) params.append('minPrice', filters.minPrice.toString())
        if (filters?.maxPrice !== undefined) params.append('maxPrice', filters.maxPrice.toString())
        if (filters?.hasPrice !== undefined) params.append('hasPrice', filters.hasPrice.toString())
        if (filters?.sortBy) params.append('sortBy', filters.sortBy)
        if (filters?.sortOrder) params.append('sortOrder', filters.sortOrder)
        if (filters?.page) params.append('page', filters.page.toString())
        if (filters?.pageSize) params.append('pageSize', filters.pageSize.toString())

        const queryString = params.toString()
        const url = `${apiBase}/api/products${queryString ? `?${queryString}` : ''}`
        return await apiFetch<PaginatedResponse<ProductListDto>>(url)
    }

    const lookupProducts = async (query: string, limit: number = 10): Promise<ProductLookupDto[]> => {
        if (!query || query.length < 3) {
            return []
        }
        const url = `${apiBase}/api/products/lookup?q=${encodeURIComponent(query)}&limit=${limit}`
        return await apiFetch<ProductLookupDto[]>(url)
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

    const compareMultipleProducts = async (productIds: string[]) => {
        return await apiFetch(`${apiBase}/api/products/compare`, {
            method: 'POST',
            body: productIds
        })
    }

    const deleteProduct = async (id: string): Promise<void> => {
        await apiFetch(`${apiBase}/api/products/${id}`, {
            method: 'DELETE'
        })
    }

    return {
        getAllProducts,
        lookupProducts,
        getProduct,
        getProductHistory,
        compareProductPrices,
        compareMultipleProducts,
        deleteProduct
    }
}
