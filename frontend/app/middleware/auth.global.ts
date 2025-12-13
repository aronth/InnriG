export default defineNuxtRouteMiddleware(async (to, from) => {
    const { getCurrentUser, isAuthenticated } = useAuth()
    
    // Public routes that don't require authentication
    const publicRoutes = ['/login', '/change-password']
    const isPublicRoute = publicRoutes.some(route => to.path === route)
    
    // If accessing a public route, allow it
    if (isPublicRoute) {
        return
    }
    
    // Try to get current user (this will set the auth state)
    const user = await getCurrentUser()
    
    // If not authenticated and not on a public route, redirect to login
    if (!user || !isAuthenticated.value) {
        return navigateTo('/login')
    }
    
    // If user must change password and not on change-password page, redirect
    if (user.mustChangePassword && to.path !== '/change-password') {
        return navigateTo('/change-password')
    }
    
    // If user has changed password but is on change-password page, redirect to home
    if (!user.mustChangePassword && to.path === '/change-password') {
        return navigateTo('/')
    }
})

