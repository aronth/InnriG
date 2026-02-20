export interface CustomerDto {
  id: string
  name: string
  phone?: string
  email?: string
  notes?: string
  createdAt: string
  updatedAt: string
}

export interface CreateCustomerDto {
  name: string
  phone?: string
  email?: string
  notes?: string
}

export interface UpdateCustomerDto {
  name: string
  phone?: string
  email?: string
  notes?: string
}

