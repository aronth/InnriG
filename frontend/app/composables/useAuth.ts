import type { User } from '~/types/user'

export const useAuth = () => {
    const config = useRuntimeConfig()
    const apiBase = config.public.apiBase
    const { apiFetch } = useApi()

    const currentUser = useState<User | null>('auth.user', () => null)
    const isAuthenticated = computed(() => currentUser.value !== null)

    const login = async (username: string, password: string): Promise<User> => {
        const user = await apiFetch<User>(`${apiBase}/api/auth/login`, {
            method: 'POST',
            body: {
                username,
                password
            }
        })
        currentUser.value = user
        return user
    }

    const logout = async (): Promise<void> => {
        try {
            await apiFetch(`${apiBase}/api/auth/logout`, {
                method: 'POST'
            })
        } catch (error) {
            // Continue with logout even if API call fails
            console.error('Logout error:', error)
        } finally {
            currentUser.value = null
            navigateTo('/login')
        }
    }

    const getCurrentUser = async (): Promise<User | null> => {
        try {
            const user = await apiFetch<User>(`${apiBase}/api/auth/me`)
            currentUser.value = user
            return user
        } catch (error) {
            currentUser.value = null
            return null
        }
    }

    const changePassword = async (currentPassword: string, newPassword: string): Promise<User> => {
        const user = await apiFetch<User>(`${apiBase}/api/auth/change-password`, {
            method: 'POST',
            body: {
                currentPassword,
                newPassword
            }
        })
        currentUser.value = user
        return user
    }

    const createUser = async (username: string, name: string): Promise<User> => {
        return await apiFetch<User>(`${apiBase}/api/auth/create-user`, {
            method: 'POST',
            body: {
                username,
                name
            }
        })
    }

    return {
        currentUser: readonly(currentUser),
        isAuthenticated,
        login,
        logout,
        getCurrentUser,
        changePassword,
        createUser
    }
}

