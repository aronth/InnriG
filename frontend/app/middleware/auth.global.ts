export default defineNuxtRouteMiddleware(async (to, from) => {
    const { getCurrentUser, isAuthenticated, hasRole } = useAuth()
    
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
    
    // Role-based route protection
    const isAdmin = hasRole('Admin')
    const isManager = hasRole('Manager') || isAdmin
    const isUser = hasRole('User') || isManager
    
    // Define route access rules
    const adminOnlyRoutes = ['/products', '/orders', '/settings/users', '/settings/system', '/suppliers', '/buyers', '/kpis', '/waittimes']
    const managerRoutes = ['/giftcards']
    const userRoutes = ['/bookings']
    
    // Check if current path requires specific roles
    const requiresAdmin = adminOnlyRoutes.some(route => to.path.startsWith(route))
    const requiresManager = managerRoutes.some(route => to.path.startsWith(route))
    const requiresUser = userRoutes.some(route => to.path.startsWith(route))
    
    // Redirect if user doesn't have required role
    if (requiresAdmin && !isAdmin) {
        return navigateTo('/unauthorized')
    }
    
    if (requiresManager && !isManager) {
        return navigateTo('/unauthorized')
    }
    
    if (requiresUser && !isUser) {
        return navigateTo('/unauthorized')
    }
})

