import type { GiftCardTemplate, CreateGiftCardTemplateDto, UpdateGiftCardTemplateDto } from '~/types/giftCard'

export const useGiftCardTemplates = () => {
    const config = useRuntimeConfig()
    const apiBase = config.public.apiBase
    const { apiFetch } = useApi()

    const getTemplates = async (isActive?: boolean): Promise<GiftCardTemplate[]> => {
        const params = new URLSearchParams()
        if (isActive !== undefined) params.append('isActive', isActive.toString())

        const queryString = params.toString()
        const url = `${apiBase}/api/giftcardtemplates${queryString ? `?${queryString}` : ''}`
        return await apiFetch<GiftCardTemplate[]>(url)
    }

    const getTemplate = async (id: string): Promise<GiftCardTemplate> => {
        return await apiFetch<GiftCardTemplate>(`${apiBase}/api/giftcardtemplates/${id}`)
    }

    const createTemplate = async (dto: CreateGiftCardTemplateDto): Promise<GiftCardTemplate> => {
        return await apiFetch<GiftCardTemplate>(`${apiBase}/api/giftcardtemplates`, {
            method: 'POST',
            body: dto
        })
    }

    const updateTemplate = async (id: string, dto: UpdateGiftCardTemplateDto): Promise<GiftCardTemplate> => {
        return await apiFetch<GiftCardTemplate>(`${apiBase}/api/giftcardtemplates/${id}`, {
            method: 'PUT',
            body: dto
        })
    }

    const deleteTemplate = async (id: string): Promise<void> => {
        await apiFetch(`${apiBase}/api/giftcardtemplates/${id}`, {
            method: 'DELETE'
        })
    }

    return {
        getTemplates,
        getTemplate,
        createTemplate,
        updateTemplate,
        deleteTemplate
    }
}



