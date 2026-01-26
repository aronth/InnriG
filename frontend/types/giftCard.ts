export type GiftCardStatus = 'Created' | 'Sold' | 'Redeemed' | 'Expired'

export interface GiftCard {
    id: string
    number: string
    templateId?: string
    templateName?: string
    restaurantId?: string
    restaurantName?: string
    restaurantCode?: string
    amount: number
    message?: string
    expirationDate?: string
    dkNumber?: string
    status: GiftCardStatus
    createdById?: string
    createdByName?: string
    soldAt?: string
    redeemedAt?: string
    printWithBackground: boolean
    createdAt: string
    updatedAt: string
}

export interface CreateGiftCardDto {
    templateId?: string
    restaurantId?: string
    amount: number
    message?: string
    dkNumber?: string
    printWithBackground?: boolean
}

export interface CreateGiftCardBatchDto {
    count: number
    templateId?: string
    restaurantId?: string
    amount?: number
    message?: string
    printWithBackground?: boolean
}

export interface GiftCardTemplate {
    id: string
    name: string
    description?: string
    defaultAmount?: number
    messageTemplate?: string
    amountAsText?: string
    isMonetaryTemplate: boolean
    restaurantId?: string
    restaurantName?: string
    isActive: boolean
    createdAt: string
    updatedAt: string
}

export interface CreateGiftCardTemplateDto {
    name: string
    description?: string
    defaultAmount?: number
    messageTemplate?: string
    amountAsText?: string
    isMonetaryTemplate?: boolean
    restaurantId?: string
    isActive?: boolean
}

export interface UpdateGiftCardTemplateDto {
    name: string
    description?: string
    defaultAmount?: number
    messageTemplate?: string
    amountAsText?: string
    isMonetaryTemplate: boolean
    restaurantId?: string
    isActive: boolean
}

export interface UpdateGiftCardStatusDto {
    status: GiftCardStatus
}

export interface GiftCardPreviewDto {
    templateId?: string
    amount: number
    message?: string
    restaurantId?: string
    dkNumber?: string
    printWithBackground: boolean
}

export interface Restaurant {
    id: string
    name: string
    code: string
    createdAt: string
    updatedAt: string
}

