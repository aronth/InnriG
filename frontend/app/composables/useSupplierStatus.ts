export interface SupplierUpdateStatus {
  supplierId: string
  supplierName: string
  lastInvoiceDate: string | null
  lastInvoiceNumber: string | null
  invoiceCount: number
  daysSinceLastInvoice: number | null
  isOverdue: boolean
  status: 'OK' | 'Overdue' | 'NoInvoices'
}

export const useSupplierStatus = () => {
  const config = useRuntimeConfig()
  const apiBase = config.public.apiBase
  const { apiFetch } = useApi()

  const getSupplierUpdateStatus = async (): Promise<SupplierUpdateStatus[]> => {
    return await apiFetch<SupplierUpdateStatus[]>(`${apiBase}/api/suppliers/update-status`)
  }

  return {
    getSupplierUpdateStatus
  }
}

