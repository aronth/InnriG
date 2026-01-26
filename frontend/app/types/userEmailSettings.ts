export interface UserEmailMappingDto {
  id: string
  emailAddress: string
  displayName?: string
  isDefault: boolean
  createdAt: string
  updatedAt: string
}

export interface CreateUserEmailMappingDto {
  emailAddress: string
  displayName?: string
  isDefault: boolean
}

export interface UpdateUserEmailMappingDto {
  displayName?: string
  isDefault: boolean
}

