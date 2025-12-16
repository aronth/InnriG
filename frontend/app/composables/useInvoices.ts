export interface InvoiceListItem {
    id: string
    supplierId: string
    buyerId?: string
    supplierName: string
    buyerName?: string
    invoiceNumber: string
    invoiceDate: string
    totalAmount: number
    createdAt: string
    itemCount: number
    supplier: {
        id: string
        name: string
    }
    buyer?: {
        id: string
        name: string
    } | null
}

export interface InvoiceFilters {
    supplierId?: string
    buyerId?: string
    startDate?: string
    endDate?: string
    sortBy?: string
    sortOrder?: 'asc' | 'desc'
}

export const useInvoices = () => {
    const config = useRuntimeConfig()
    const apiBase = config.public.apiBase
    const { apiFetch } = useApi()

    const getAllInvoices = async (filters?: InvoiceFilters): Promise<InvoiceListItem[]> => {
        const params = new URLSearchParams()
        
        if (filters?.supplierId) params.append('supplierId', filters.supplierId)
        if (filters?.buyerId) params.append('buyerId', filters.buyerId)
        if (filters?.startDate) params.append('startDate', filters.startDate)
        if (filters?.endDate) params.append('endDate', filters.endDate)
        if (filters?.sortBy) params.append('sortBy', filters.sortBy)
        if (filters?.sortOrder) params.append('sortOrder', filters.sortOrder)

        const queryString = params.toString()
        const url = `${apiBase}/api/invoices${queryString ? `?${queryString}` : ''}`
        return await apiFetch<InvoiceListItem[]>(url)
    }

    const getInvoice = async (id: string) => {
        return await apiFetch(`${apiBase}/api/invoices/${id}`)
    }

    const deleteInvoice = async (id: string): Promise<void> => {
        await apiFetch(`${apiBase}/api/invoices/${id}`, {
            method: 'DELETE'
        })
    }

    const bulkUploadInvoices = async (files: File[]): Promise<BulkUploadResult> => {
        const formData = new FormData()
        files.forEach(file => {
            formData.append('files', file)
        })

        return await apiFetch<BulkUploadResult>(`${apiBase}/api/invoices/bulk-upload`, {
            method: 'POST',
            body: formData
        })
    }

    return {
        getAllInvoices,
        getInvoice,
        deleteInvoice,
        bulkUploadInvoices
    }
}

export interface BulkUploadResult {
    total: number
    successful: number
    skipped: number
    failed: number
    errors: string[]
}

