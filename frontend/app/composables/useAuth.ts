import type { User } from '~/types/user'

export const useAuth = () => {
    const config = useRuntimeConfig()
    const apiBase = config.public.apiBase
    const { apiFetch } = useApi()

    const currentUser = useState<User | null>('auth.user', () => null)
    const isAuthenticated = computed(() => currentUser.value !== null)

    const login = async (username: string, password: string): Promise<{ user: User; cookieSet: boolean }> => {
        const response = await apiFetch<{ user: User; cookieSet: boolean }>(`${apiBase}/api/auth/login`, {
            method: 'POST',
            body: {
                username,
                password
            }
        })
        currentUser.value = response.user
        return response
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

    const createUser = async (username: string, name: string, role?: string): Promise<User> => {
        return await apiFetch<User>(`${apiBase}/api/auth/create-user`, {
            method: 'POST',
            body: {
                username,
                name,
                role
            }
        })
    }

    const assignRole = async (userId: string, role: string): Promise<User> => {
        return await apiFetch<User>(`${apiBase}/api/auth/assign-role`, {
            method: 'POST',
            body: {
                userId,
                role
            }
        })
    }

    const removeRole = async (userId: string, role: string): Promise<User> => {
        return await apiFetch<User>(`${apiBase}/api/auth/remove-role`, {
            method: 'POST',
            body: {
                userId,
                role
            }
        })
    }

    const getAvailableRoles = async (): Promise<string[]> => {
        return await apiFetch<string[]>(`${apiBase}/api/auth/roles`)
    }

    const updateProfile = async (name: string): Promise<User> => {
        const user = await apiFetch<User>(`${apiBase}/api/auth/profile`, {
            method: 'PUT',
            body: {
                name
            }
        })
        currentUser.value = user
        return user
    }

    // Role checking helpers
    const hasRole = (role: string): boolean => {
        return currentUser.value?.roles?.includes(role) ?? false
    }

    const isSystemAdmin = computed(() => hasRole('SystemAdmin'))
    const isAdmin = computed(() => hasRole('Admin') || isSystemAdmin.value)
    const isManager = computed(() => hasRole('Manager') || isAdmin.value)
    const isUser = computed(() => hasRole('User') || isManager.value)

    // Can access specific features
    const canAccessBookings = computed(() => isUser.value)
    const canAccessGiftCards = computed(() => isManager.value)
    const canAccessAdmin = computed(() => isAdmin.value)
    const canAccessSystemAdmin = computed(() => isSystemAdmin.value)

    return {
        currentUser: readonly(currentUser),
        isAuthenticated,
        login,
        logout,
        getCurrentUser,
        changePassword,
        updateProfile,
        createUser,
        assignRole,
        removeRole,
        getAvailableRoles,
        // Role checks
        hasRole,
        isSystemAdmin,
        isAdmin,
        isManager,
        isUser,
        canAccessBookings,
        canAccessGiftCards,
        canAccessAdmin,
        canAccessSystemAdmin
    }
}

