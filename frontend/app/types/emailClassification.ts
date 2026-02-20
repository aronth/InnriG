export interface EmailClassification {
  id: string
  name: string
  description: string
  systemPrompt?: string | null
  isSystem: boolean
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface CreateEmailClassificationDto {
  name: string
  description: string
  systemPrompt?: string | null
}

export interface UpdateEmailClassificationDto {
  name: string
  description: string
  systemPrompt?: string | null
  isActive: boolean
}

