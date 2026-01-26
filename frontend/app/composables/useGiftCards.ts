import type { GiftCard, CreateGiftCardDto, CreateGiftCardBatchDto, UpdateGiftCardStatusDto, GiftCardStatus, GiftCardPreviewDto } from '~/types/giftCard'

export const useGiftCards = () => {
    const config = useRuntimeConfig()
    const apiBase = config.public.apiBase
    const { apiFetch } = useApi()

    const getGiftCards = async (filters?: {
        status?: GiftCardStatus
        templateId?: string
        fromDate?: string
        toDate?: string
    }): Promise<GiftCard[]> => {
        const params = new URLSearchParams()
        
        if (filters?.status) params.append('status', filters.status)
        if (filters?.templateId) params.append('templateId', filters.templateId)
        if (filters?.fromDate) params.append('fromDate', filters.fromDate)
        if (filters?.toDate) params.append('toDate', filters.toDate)

        const queryString = params.toString()
        const url = `${apiBase}/api/giftcards${queryString ? `?${queryString}` : ''}`
        return await apiFetch<GiftCard[]>(url)
    }

    const getGiftCard = async (id: string): Promise<GiftCard> => {
        return await apiFetch<GiftCard>(`${apiBase}/api/giftcards/${id}`)
    }

    const createGiftCard = async (dto: CreateGiftCardDto): Promise<GiftCard> => {
        return await apiFetch<GiftCard>(`${apiBase}/api/giftcards`, {
            method: 'POST',
            body: dto
        })
    }

    const createGiftCardsBatch = async (dto: CreateGiftCardBatchDto): Promise<GiftCard[]> => {
        return await apiFetch<GiftCard[]>(`${apiBase}/api/giftcards/batch`, {
            method: 'POST',
            body: dto
        })
    }

    const updateGiftCard = async (id: string, dto: CreateGiftCardDto): Promise<GiftCard> => {
        return await apiFetch<GiftCard>(`${apiBase}/api/giftcards/${id}`, {
            method: 'PUT',
            body: dto
        })
    }

    const updateGiftCardStatus = async (id: string, status: GiftCardStatus): Promise<GiftCard> => {
        return await apiFetch<GiftCard>(`${apiBase}/api/giftcards/${id}/status`, {
            method: 'PUT',
            body: { status }
        })
    }

    const generatePdf = async (id: string, includeBackground: boolean = false): Promise<Blob> => {
        const response = await fetch(
            `${apiBase}/api/giftcards/${id}/pdf?includeBackground=${includeBackground}`,
            {
                credentials: 'include'
            }
        )
        
        if (!response.ok) {
            throw new Error('Failed to generate PDF')
        }
        
        return await response.blob()
    }

    const downloadPdf = async (id: string, includeBackground: boolean = false): Promise<void> => {
        const blob = await generatePdf(id, includeBackground)
        const url = window.URL.createObjectURL(blob)
        const a = document.createElement('a')
        a.href = url
        a.download = `gjafakort_${id}_${new Date().toISOString().slice(0, 10)}.pdf`
        document.body.appendChild(a)
        a.click()
        window.URL.revokeObjectURL(url)
        document.body.removeChild(a)
    }

    const previewGiftCard = async (dto: GiftCardPreviewDto): Promise<Blob> => {
        const response = await fetch(`${apiBase}/api/giftcards/preview`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(dto)
        })
        
        if (!response.ok) throw new Error('Failed to generate preview')
        return await response.blob()
    }

    return {
        getGiftCards,
        getGiftCard,
        createGiftCard,
        createGiftCardsBatch,
        updateGiftCard,
        updateGiftCardStatus,
        generatePdf,
        downloadPdf,
        previewGiftCard
    }
}



