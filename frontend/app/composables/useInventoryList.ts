export interface UnifiedInventoryListItem {
  birgir: string
  vorunumer: string
  voruheiti: string
  eining: string
  verdAnVsk: number
  skilagjoldUmbudagjold: number
  nettoInnkaupsverdPerEiningu: number
  dagsetningSidustuUppfaerslu: string
  sidastiReikningsnumer?: string
}

export const useInventoryList = () => {
  const config = useRuntimeConfig()
  const apiBase = config.public.apiBase
  const { apiFetch } = useApi()

  const getUnifiedList = async (supplierId?: string, search?: string): Promise<UnifiedInventoryListItem[]> => {
    const params = new URLSearchParams()
    if (supplierId) params.append('supplierId', supplierId)
    if (search) params.append('search', search)

    return await apiFetch<UnifiedInventoryListItem[]>(
      `${apiBase}/api/products/unified-inventory-list?${params.toString()}`
    )
  }

  const exportToCsv = async (supplierId?: string, search?: string) => {
    const params = new URLSearchParams()
    if (supplierId) params.append('supplierId', supplierId)
    if (search) params.append('search', search)

    const response = await fetch(
      `${apiBase}/api/products/unified-inventory-list/export?${params.toString()}`,
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
    a.download = `vorulisti_${new Date().toISOString().slice(0, 10)}.csv`
    document.body.appendChild(a)
    a.click()
    window.URL.revokeObjectURL(url)
    document.body.removeChild(a)
  }

  return {
    getUnifiedList,
    exportToCsv
  }
}

