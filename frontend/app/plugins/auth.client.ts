export default defineNuxtPlugin(async () => {
    // Only run on client side
    if (process.server) return

    const { getCurrentUser } = useAuth()
    
    // Initialize auth state on app startup
    // This ensures the user is loaded from the cookie on page refresh
    try {
        await getCurrentUser()
    } catch (error) {
        // Silently fail - user might not be authenticated
        console.debug('Auth initialization:', error)
    }
})

