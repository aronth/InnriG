import type { UserEmailMappingDto, CreateUserEmailMappingDto, UpdateUserEmailMappingDto } from '~/types/userEmailSettings'

export const useUserEmailSettings = () => {
  const { apiFetch } = useApi()

  const getEmailMappings = async () => {
    return await apiFetch<UserEmailMappingDto[]>('/api/user-email-settings/email-mappings')
  }

  const createEmailMapping = async (dto: CreateUserEmailMappingDto) => {
    return await apiFetch<UserEmailMappingDto>('/api/user-email-settings/email-mappings', {
      method: 'POST',
      body: dto
    })
  }

  const updateEmailMapping = async (id: string, dto: UpdateUserEmailMappingDto) => {
    return await apiFetch<UserEmailMappingDto>(`/api/user-email-settings/email-mappings/${id}`, {
      method: 'PUT',
      body: dto
    })
  }

  const deleteEmailMapping = async (id: string) => {
    return await apiFetch(`/api/user-email-settings/email-mappings/${id}`, {
      method: 'DELETE'
    })
  }

  const getEmailSignature = async () => {
    const result = await apiFetch<{ signature: string }>('/api/user-email-settings/signature')
    return result.signature
  }

  const updateEmailSignature = async (signature: string) => {
    const result = await apiFetch<{ signature: string }>('/api/user-email-settings/signature', {
      method: 'PUT',
      body: { signature }
    })
    return result.signature
  }

  return {
    getEmailMappings,
    createEmailMapping,
    updateEmailMapping,
    deleteEmailMapping,
    getEmailSignature,
    updateEmailSignature
  }
}

