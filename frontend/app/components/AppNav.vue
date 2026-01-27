<template>
  <nav class="sticky top-0 z-50 w-full backdrop-blur-md bg-white/70 border-b border-gray-100 shadow-sm transition-all duration-300">
    <div class="px-4">
      <div class="flex justify-between h-16">
        <div class="flex items-center gap-8">
          <!-- Logo / Brand -->
          <div class="flex-shrink-0 flex items-center cursor-pointer transition-transform hover:scale-[1.02]" @click="$router.push('/')">
            <!-- Icon could go here, for now using just text -->
            <div class="w-8 h-8 bg-indigo-600 rounded-lg flex items-center justify-center mr-3 shadow-indigo-200 shadow-md">
              <span class="text-white font-bold text-lg">I</span>
            </div>
            <h1 class="text-xl font-bold bg-clip-text text-transparent bg-gradient-to-r from-gray-900 to-gray-600 tracking-tight font-display">
              InnriGreifinn
            </h1>
          </div>

          <!-- Desktop Navigation -->
          <div class="hidden sm:flex sm:space-x-1">
            <!-- Bookings - Available to all authenticated users (User, Manager, Admin) -->
            <NuxtLink 
              v-if="canAccessBookings"
              to="/bookings" 
              class="inline-flex items-center px-4 py-2 text-sm font-medium rounded-md transition-colors duration-200"
              :class="[$route.path.startsWith('/bookings') && !$route.path.startsWith('/bookings/table') ? 'bg-indigo-50 text-indigo-700' : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900']"
            >
              Bókanir
            </NuxtLink>
            <NuxtLink 
              v-if="canAccessBookings"
              to="/bookings/table" 
              class="inline-flex items-center px-4 py-2 text-sm font-medium rounded-md transition-colors duration-200"
              :class="[$route.path.startsWith('/bookings/table') ? 'bg-indigo-50 text-indigo-700' : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900']"
            >
              Borðapantanir
            </NuxtLink>
            
            <!-- Gift Cards - Hidden for now -->
            <!-- <NuxtLink 
              v-if="canAccessGiftCards"
              to="/giftcards" 
              class="inline-flex items-center px-4 py-2 text-sm font-medium rounded-md transition-colors duration-200"
              :class="[$route.path.startsWith('/giftcards') ? 'bg-indigo-50 text-indigo-700' : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900']"
            >
              Gjafakort
            </NuxtLink> -->
            
            <!-- Admin-only sections -->
            <NuxtLink 
              v-if="canAccessAdmin"
              to="/products" 
              class="inline-flex items-center px-4 py-2 text-sm font-medium rounded-md transition-colors duration-200"
              :class="[$route.path.startsWith('/products') ? 'bg-indigo-50 text-indigo-700' : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900']"
            >
              Verðgreining
            </NuxtLink>
            <NuxtLink 
              v-if="canAccessAdmin"
              to="/orders" 
              class="inline-flex items-center px-4 py-2 text-sm font-medium rounded-md transition-colors duration-200"
              :class="[$route.path.startsWith('/orders') ? 'bg-indigo-50 text-indigo-700' : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900']"
            >
              Pantanir
            </NuxtLink>
            <NuxtLink 
              v-if="canAccessAdmin"
              to="/timaskyrsla" 
              class="inline-flex items-center px-4 py-2 text-sm font-medium rounded-md transition-colors duration-200"
              :class="[$route.path.startsWith('/timaskyrsla') ? 'bg-indigo-50 text-indigo-700' : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900']"
            >
              Tímaskýrsla
            </NuxtLink>
            <NuxtLink 
              v-if="canAccessAdmin"
              to="/users" 
              class="inline-flex items-center px-4 py-2 text-sm font-medium rounded-md transition-colors duration-200"
              :class="[$route.path.startsWith('/users') ? 'bg-indigo-50 text-indigo-700' : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900']"
            >
              Notendur
            </NuxtLink>
            <NuxtLink 
              to="/emails" 
              class="inline-flex items-center px-4 py-2 text-sm font-medium rounded-md transition-colors duration-200"
              :class="[$route.path.startsWith('/emails') ? 'bg-indigo-50 text-indigo-700' : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900']"
            >
              Tölvupóstur
            </NuxtLink>
          </div>
        </div>

        <!-- Right Side User/Actions -->
        <div class="flex items-center gap-4">
          <div v-if="currentUser" class="relative">
            <!-- User Menu Button -->
            <button
              ref="userMenuButton"
              @click="showUserMenu = !showUserMenu"
              class="flex items-center gap-3 px-3 py-2 rounded-lg hover:bg-gray-50 transition-colors duration-200 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2"
            >
              <div class="h-9 w-9 rounded-full bg-gradient-to-br from-indigo-500 to-purple-600 border-2 border-white shadow-md flex items-center justify-center text-white font-semibold text-sm">
                {{ getUserInitials(currentUser.name) }}
              </div>
              <div class="text-left hidden sm:block">
                <p class="text-sm font-medium text-gray-900">{{ currentUser.name }}</p>
                <p class="text-xs text-gray-500">{{ currentUser.username }}</p>
              </div>
              <svg 
                class="w-4 h-4 text-gray-400 transition-transform duration-200"
                :class="{ 'rotate-180': showUserMenu }"
                fill="none" 
                stroke="currentColor" 
                viewBox="0 0 24 24"
              >
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
              </svg>
            </button>

            <!-- Dropdown Menu -->
            <Transition
              enter-active-class="transition ease-out duration-100"
              enter-from-class="transform opacity-0 scale-95"
              enter-to-class="transform opacity-100 scale-100"
              leave-active-class="transition ease-in duration-75"
              leave-from-class="transform opacity-100 scale-100"
              leave-to-class="transform opacity-0 scale-95"
            >
              <div
                v-if="showUserMenu"
                ref="userMenuDropdown"
                class="absolute right-0 mt-2 w-56 rounded-lg shadow-lg bg-white ring-1 ring-black ring-opacity-5 z-50"
              >
                <div class="py-1">
                  <!-- User Info Section -->
                  <div class="px-4 py-3 border-b border-gray-100">
                    <p class="text-sm font-medium text-gray-900">{{ currentUser.name }}</p>
                    <p class="text-xs text-gray-500 mt-0.5">{{ currentUser.username }}</p>
                  </div>

                  <!-- Menu Items -->
                  <NuxtLink
                    to="/settings"
                    @click="showUserMenu = false"
                    class="flex items-center px-4 py-2 text-sm text-gray-700 hover:bg-gray-50 transition-colors duration-150"
                  >
                    <svg class="w-5 h-5 mr-3 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                    </svg>
                    Stillingar
                  </NuxtLink>

                  <div class="border-t border-gray-100 my-1"></div>

                  <button
                    @click="handleLogout"
                    class="w-full flex items-center px-4 py-2 text-sm text-gray-700 hover:bg-gray-50 transition-colors duration-150"
                  >
                    <svg class="w-5 h-5 mr-3 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
                    </svg>
                    Skrá út
                  </button>
                </div>
              </div>
            </Transition>
          </div>
        </div>
      </div>
    </div>
  </nav>
</template>

<script setup lang="ts">
const route = useRoute()
const { 
  currentUser, 
  logout, 
  canAccessBookings, 
  canAccessGiftCards, 
  canAccessAdmin 
} = useAuth()

const showUserMenu = ref(false)
const userMenuButton = ref<HTMLElement | null>(null)
const userMenuDropdown = ref<HTMLElement | null>(null)

const getUserInitials = (name: string): string => {
  if (!name) return '?'
  const parts = name.trim().split(' ')
  if (parts.length === 1) {
    return parts[0].charAt(0).toUpperCase()
  }
  return (parts[0].charAt(0) + parts[parts.length - 1].charAt(0)).toUpperCase()
}

const handleClickOutside = (event: MouseEvent) => {
  if (
    showUserMenu.value &&
    userMenuButton.value &&
    userMenuDropdown.value &&
    !userMenuButton.value.contains(event.target as Node) &&
    !userMenuDropdown.value.contains(event.target as Node)
  ) {
    showUserMenu.value = false
  }
}

onMounted(() => {
  document.addEventListener('click', handleClickOutside)
})

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside)
})

const handleLogout = async () => {
  showUserMenu.value = false
  await logout()
}
</script>

<style scoped>
/* Optional: slightly distinct font for the brand if not using global yet */
.font-display {
  font-family: 'Inter', system-ui, -apple-system, sans-serif;
}
</style>
