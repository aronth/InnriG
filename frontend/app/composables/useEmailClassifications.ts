import type { EmailClassification, CreateEmailClassificationDto, UpdateEmailClassificationDto } from '~/types/emailClassification'

export const useEmailClassifications = () => {
  const config = useRuntimeConfig()

  const getAll = async (isActive?: boolean, isSystem?: boolean): Promise<EmailClassification[]> => {
    const params = new URLSearchParams()
    if (isActive !== undefined) params.append('isActive', String(isActive))
    if (isSystem !== undefined) params.append('isSystem', String(isSystem))
    
    const query = params.toString()
    const url = query ? `/api/email-classifications?${query}` : '/api/email-classifications'
    
    return await $fetch<EmailClassification[]>(url, {
      baseURL: config.public.apiBase
    })
  }

  const getActive = async (): Promise<EmailClassification[]> => {
    return await $fetch<EmailClassification[]>('/api/email-classifications/active', {
      baseURL: config.public.apiBase
    })
  }

  const getById = async (id: string): Promise<EmailClassification> => {
    return await $fetch<EmailClassification>(`/api/email-classifications/${id}`, {
      baseURL: config.public.apiBase
    })
  }

  const create = async (dto: CreateEmailClassificationDto): Promise<EmailClassification> => {
    return await $fetch<EmailClassification>('/api/email-classifications', {
      method: 'POST',
      body: dto,
      baseURL: config.public.apiBase
    })
  }

  const update = async (id: string, dto: UpdateEmailClassificationDto): Promise<EmailClassification> => {
    return await $fetch<EmailClassification>(`/api/email-classifications/${id}`, {
      method: 'PUT',
      body: dto,
      baseURL: config.public.apiBase
    })
  }

  const deleteClassification = async (id: string): Promise<void> => {
    await $fetch(`/api/email-classifications/${id}`, {
      method: 'DELETE',
      baseURL: config.public.apiBase
    })
  }

  return {
    getAll,
    getActive,
    getById,
    create,
    update,
    delete: deleteClassification
  }
}

