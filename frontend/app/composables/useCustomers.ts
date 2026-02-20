import type { CustomerDto, CreateCustomerDto, UpdateCustomerDto } from '~/types/customer'

export const useCustomers = () => {
  const config = useRuntimeConfig()
  const apiBase = config.public.apiBase
  const { apiFetch } = useApi()

  const getAllCustomers = async (): Promise<CustomerDto[]> => {
    return await apiFetch<CustomerDto[]>(`${apiBase}/api/customers`)
  }

  const getCustomerById = async (id: string): Promise<CustomerDto> => {
    return await apiFetch<CustomerDto>(`${apiBase}/api/customers/${id}`)
  }

  const getCustomerByPhone = async (phone: string): Promise<CustomerDto | null> => {
    try {
      return await apiFetch<CustomerDto>(`${apiBase}/api/customers/phone/${encodeURIComponent(phone)}`)
    } catch (error: any) {
      if (error.status === 404 || error.statusCode === 404) {
        return null
      }
      throw error
    }
  }

  const createCustomer = async (dto: CreateCustomerDto): Promise<CustomerDto> => {
    return await apiFetch<CustomerDto>(`${apiBase}/api/customers`, {
      method: 'POST',
      body: dto
    })
  }

  const updateCustomer = async (id: string, dto: UpdateCustomerDto): Promise<CustomerDto> => {
    return await apiFetch<CustomerDto>(`${apiBase}/api/customers/${id}`, {
      method: 'PUT',
      body: dto
    })
  }

  const deleteCustomer = async (id: string): Promise<void> => {
    await apiFetch(`${apiBase}/api/customers/${id}`, {
      method: 'DELETE'
    })
  }

  return {
    getAllCustomers,
    getCustomerById,
    getCustomerByPhone,
    createCustomer,
    updateCustomer,
    deleteCustomer
  }
}

