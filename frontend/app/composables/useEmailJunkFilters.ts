export interface EmailJunkFilterDto {
  id: string
  subject?: string | null
  senderEmail?: string | null
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface CreateEmailJunkFilterDto {
  subject?: string | null
  senderEmail?: string | null
  isActive?: boolean
}

export interface UpdateEmailJunkFilterDto {
  subject?: string | null
  senderEmail?: string | null
  isActive?: boolean
}

export const useEmailJunkFilters = () => {
  const { apiFetch } = useApi()

  const getJunkFilters = async (): Promise<EmailJunkFilterDto[]> => {
    return await apiFetch<EmailJunkFilterDto[]>('/api/emails/junk-filters')
  }

  const getJunkFilter = async (id: string): Promise<EmailJunkFilterDto> => {
    return await apiFetch<EmailJunkFilterDto>(`/api/emails/junk-filters/${id}`)
  }

  const createJunkFilter = async (dto: CreateEmailJunkFilterDto): Promise<EmailJunkFilterDto> => {
    return await apiFetch<EmailJunkFilterDto>('/api/emails/junk-filters', {
      method: 'POST',
      body: dto
    })
  }

  const updateJunkFilter = async (id: string, dto: UpdateEmailJunkFilterDto): Promise<EmailJunkFilterDto> => {
    return await apiFetch<EmailJunkFilterDto>(`/api/emails/junk-filters/${id}`, {
      method: 'PUT',
      body: dto
    })
  }

  const deleteJunkFilter = async (id: string): Promise<void> => {
    await apiFetch(`/api/emails/junk-filters/${id}`, {
      method: 'DELETE'
    })
  }

  return {
    getJunkFilters,
    getJunkFilter,
    createJunkFilter,
    updateJunkFilter,
    deleteJunkFilter
  }
}

