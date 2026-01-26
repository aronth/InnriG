import type { EmailConversationDto, EmailMessageBodyDto } from '~/types/email'

export const useEmails = () => {
  const { apiFetch } = useApi()

  const getConversations = async (params?: {
    status?: string
    assignedTo?: string
    classification?: string
    page?: number
    pageSize?: number
  }) => {
    const queryParams = new URLSearchParams()
    if (params?.status) queryParams.append('status', params.status)
    if (params?.assignedTo) queryParams.append('assignedTo', params.assignedTo)
    if (params?.classification) queryParams.append('classification', params.classification)
    if (params?.page) queryParams.append('page', params.page.toString())
    if (params?.pageSize) queryParams.append('pageSize', params.pageSize.toString())

    const query = queryParams.toString()
    return await apiFetch<{
      items: EmailConversationDto[]
      totalCount: number
      page: number
      pageSize: number
      totalPages: number
    }>(`/api/emails/conversations${query ? `?${query}` : ''}`)
  }

  const getConversation = async (id: string) => {
    return await apiFetch<EmailConversationDto>(`/api/emails/conversations/${id}`)
  }

  const getMessageBody = async (id: string) => {
    return await apiFetch<EmailMessageBodyDto>(`/api/emails/messages/${id}/body`)
  }

  const assignConversation = async (id: string, userId: string | null) => {
    return await apiFetch(`/api/emails/conversations/${id}/assign`, {
      method: 'PUT',
      body: { userId }
    })
  }

  const updateStatus = async (id: string, status: string) => {
    return await apiFetch(`/api/emails/conversations/${id}/status`, {
      method: 'PUT',
      body: { status }
    })
  }

  const reparseConversation = async (id: string) => {
    return await apiFetch(`/api/emails/conversations/${id}/reparse`, {
      method: 'POST'
    })
  }

  const regenerateAIAnalysis = async (messageId: string) => {
    return await apiFetch<EmailConversationDto>(`/api/emails/messages/${messageId}/regenerate-ai`, {
      method: 'POST'
    })
  }

  const replyToConversation = async (
    conversationId: string,
    body: string,
    fromEmailAddress?: string,
    cc?: string,
    bcc?: string
  ) => {
    return await apiFetch<EmailConversationDto>(`/api/emails/conversations/${conversationId}/reply`, {
      method: 'POST',
      body: {
        body,
        fromEmailAddress,
        cc,
        bcc
      }
    })
  }

  return {
    getConversations,
    getConversation,
    getMessageBody,
    assignConversation,
    updateStatus,
    reparseConversation,
    regenerateAIAnalysis,
    replyToConversation
  }
}

