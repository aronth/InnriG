import type { EmailConnectionDto, EmailConnectionStatusDto, ConnectEmailRequestDto, PollTokenResponseDto } from '~/types/email'

export const useEmailAuth = () => {
  const { apiFetch } = useApi()

  const connectEmail = async (emailAddress: string, isSystemInbox: boolean): Promise<EmailConnectionDto> => {
    const request: ConnectEmailRequestDto = {
      emailAddress,
      isSystemInbox
    }
    return await apiFetch<EmailConnectionDto>('/api/auth/email/connect', {
      method: 'POST',
      body: request
    })
  }

  const pollForToken = async (deviceCode: string): Promise<PollTokenResponseDto> => {
    return await apiFetch<PollTokenResponseDto>('/api/auth/email/poll', {
      method: 'POST',
      body: { deviceCode }
    })
  }

  const getConnectionStatus = async (): Promise<EmailConnectionStatusDto[]> => {
    return await apiFetch<EmailConnectionStatusDto[]>('/api/auth/email/status')
  }

  const disconnectEmail = async (emailAddress: string): Promise<void> => {
    await apiFetch(`/api/auth/email/disconnect/${encodeURIComponent(emailAddress)}`, {
      method: 'DELETE'
    })
  }

  const setSystemInbox = async (emailAddress: string, isSystemInbox: boolean): Promise<void> => {
    await apiFetch('/api/auth/email/set-system-inbox', {
      method: 'PUT',
      body: {
        emailAddress,
        isSystemInbox
      }
    })
  }

  return {
    connectEmail,
    pollForToken,
    getConnectionStatus,
    disconnectEmail,
    setSystemInbox
  }
}

