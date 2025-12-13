import type { User } from '~/types/user'

export const useUsers = () => {
    const config = useRuntimeConfig()
    const apiBase = config.public.apiBase
    const { apiFetch } = useApi()

    const getAllUsers = async (): Promise<User[]> => {
        return await apiFetch<User[]>(`${apiBase}/api/users`)
    }

    const getUser = async (id: string): Promise<User> => {
        return await apiFetch<User>(`${apiBase}/api/users/${id}`)
    }

    const updateUser = async (id: string, name: string): Promise<User> => {
        return await apiFetch<User>(`${apiBase}/api/users/${id}`, {
            method: 'PUT',
            body: {
                username: '', // Not used in update, but required by DTO
                name
            }
        })
    }

    const deleteUser = async (id: string): Promise<void> => {
        await apiFetch(`${apiBase}/api/users/${id}`, {
            method: 'DELETE'
        })
    }

    return {
        getAllUsers,
        getUser,
        updateUser,
        deleteUser
    }
}

