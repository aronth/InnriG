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
            <NuxtLink 
              to="/" 
              class="inline-flex items-center px-4 py-2 text-sm font-medium rounded-md transition-colors duration-200"
              :class="[$route.path === '/' ? 'bg-indigo-50 text-indigo-700' : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900']"
            >
              Stjórnborð
            </NuxtLink>
            <NuxtLink 
              to="/products" 
              class="inline-flex items-center px-4 py-2 text-sm font-medium rounded-md transition-colors duration-200"
              :class="[$route.path.startsWith('/products') ? 'bg-indigo-50 text-indigo-700' : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900']"
            >
              Verðgreining
            </NuxtLink>
            <NuxtLink 
              to="/invoices" 
              class="inline-flex items-center px-4 py-2 text-sm font-medium rounded-md transition-colors duration-200"
              :class="[$route.path.startsWith('/invoices') ? 'bg-indigo-50 text-indigo-700' : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900']"
            >
              Reikningar
            </NuxtLink>
            <NuxtLink 
              to="/users" 
              class="inline-flex items-center px-4 py-2 text-sm font-medium rounded-md transition-colors duration-200"
              :class="[$route.path.startsWith('/users') ? 'bg-indigo-50 text-indigo-700' : 'text-gray-600 hover:bg-gray-50 hover:text-gray-900']"
            >
              Notendur
            </NuxtLink>
          </div>
        </div>

        <!-- Right Side User/Actions -->
        <div class="flex items-center gap-4">
          <div v-if="currentUser" class="flex items-center gap-3">
            <div class="text-right hidden sm:block">
              <p class="text-sm font-medium text-gray-900">{{ currentUser.name }}</p>
              <p class="text-xs text-gray-500">{{ currentUser.username }}</p>
            </div>
            <div class="h-8 w-8 rounded-full bg-indigo-100 border border-indigo-200 flex items-center justify-center text-indigo-600">
              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor" class="w-5 h-5">
                <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-5.5-2.5a2.5 2.5 0 11-5 0 2.5 2.5 0 015 0zM10 12a5.99 5.99 0 00-4.793 2.39A9.948 9.948 0 0110 5a9.948 9.948 0 014.793 11.39A5.99 5.99 0 0010 12z" clip-rule="evenodd" />
              </svg>
            </div>
            <button
              @click="handleLogout"
              class="inline-flex items-center px-3 py-2 text-sm font-medium text-gray-700 hover:text-gray-900 hover:bg-gray-50 rounded-md transition-colors duration-200"
            >
              <svg class="w-5 h-5 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
              </svg>
              <span class="hidden sm:inline">Skrá út</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  </nav>
</template>

<script setup lang="ts">
const route = useRoute()
const { currentUser, logout } = useAuth()

const handleLogout = async () => {
  await logout()
}
</script>

<style scoped>
/* Optional: slightly distinct font for the brand if not using global yet */
.font-display {
  font-family: 'Inter', system-ui, -apple-system, sans-serif;
}
</style>
