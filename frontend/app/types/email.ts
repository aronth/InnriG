export interface EmailConversationDto {
  id: string
  graphConversationId: string
  subject: string
  fromEmail: string
  fromName: string
  status: string
  assignedToUserId?: string
  assignedToName?: string
  classification?: string
  priority: string
  messageCount: number
  lastMessageReceivedAt: string
  createdAt: string
  updatedAt: string
  extractedData?: EmailExtractedDataDto
  messages?: EmailMessageDto[]
}

export interface EmailMessageDto {
  id: string
  conversationId: string
  graphMessageId: string
  subject: string
  fromEmail: string
  fromName: string
  toEmail: string
  toName: string
  cc?: string
  bcc?: string
  receivedDateTime: string
  sentDateTime: string
  isRead: boolean
  isOutgoing: boolean
  hasAttachments: boolean
  attachmentCount: number
  importance?: string
  isAIResponse?: boolean
  messageBody?: string
  createdAt: string
  classificationQueueStatus?: string
  classificationQueuedAt?: string
  classificationCompletedAt?: string
}

export interface EmailMessageBodyDto {
  html?: string
  text?: string
  contentType: string
}

export interface EmailExtractedDataDto {
  id: string
  classification: string
  confidence: number
  requestedDate?: string
  requestedTime?: string
  guestCount?: number
  adultCount?: number
  childCount?: number
  locationCode?: string
  specialRequests?: string
  contactPhone?: string
  contactEmail?: string
  contactName?: string
  extractedAt: string
}

