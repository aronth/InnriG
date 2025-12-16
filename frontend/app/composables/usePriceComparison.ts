export interface PriceComparisonDto {
  productId: string
  productCode: string
  productName: string
  supplierId: string
  supplierName: string
  unit: string
  fromDate: string
  fromUnitPrice: number
  fromListPrice: number
  fromDiscount: number
  fromInvoiceNumber: string
  toDate: string
  toUnitPrice: number
  toListPrice: number
  toDiscount: number
  toInvoiceNumber: string
  unitPriceChange: number
  unitPriceChangePercent: number
  listPriceChange: number
  listPriceChangePercent: number
  discountChange: number
  discountChangePercent: number
}

export interface PriceComparisonSummaryDto {
  totalProducts: number
  productsWithPriceIncrease: number
  productsWithPriceDecrease: number
  productsWithNoChange: number
  averagePriceChangePercent: number
  averagePriceIncrease: number
  averagePriceDecrease: number
}

export interface PriceComparisonResponse {
  fromDate: string
  toDate: string
  summary: PriceComparisonSummaryDto
  products: PriceComparisonDto[]
}

export const usePriceComparison = () => {
  const config = useRuntimeConfig()
  const apiBase = config.public.apiBase
  const { apiFetch } = useApi()

  const getPriceComparison = async (
    fromDate: string,
    toDate: string,
    supplierId?: string
  ): Promise<PriceComparisonResponse> => {
    const params = new URLSearchParams()
    params.append('fromDate', fromDate)
    params.append('toDate', toDate)
    if (supplierId) params.append('supplierId', supplierId)

    return await apiFetch<PriceComparisonResponse>(
      `${apiBase}/api/products/price-comparison?${params.toString()}`
    )
  }

  const exportToCsv = async (
    fromDate: string,
    toDate: string,
    supplierId?: string
  ) => {
    const params = new URLSearchParams()
    params.append('fromDate', fromDate)
    params.append('toDate', toDate)
    if (supplierId) params.append('supplierId', supplierId)

    const response = await fetch(
      `${apiBase}/api/products/price-comparison/export?${params.toString()}`,
      {
        credentials: 'include'
      }
    )
    
    if (!response.ok) {
      throw new Error('Failed to export CSV')
    }
    
    const blob = await response.blob()
    const url = window.URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `verdbreytingaskyrsla_${fromDate}_${toDate}.csv`
    document.body.appendChild(a)
    a.click()
    window.URL.revokeObjectURL(url)
    document.body.removeChild(a)
  }

  return {
    getPriceComparison,
    exportToCsv
  }
}

