<template>
  <div class="min-h-screen flex items-center justify-center bg-gradient-to-br from-indigo-50 via-purple-50 to-pink-50 py-12 px-4 sm:px-6 lg:px-8">
    <div class="max-w-md w-full space-y-8">
      <div>
        <div class="flex justify-center">
          <div class="w-16 h-16 bg-indigo-600 rounded-xl flex items-center justify-center shadow-lg">
            <span class="text-white font-bold text-2xl">I</span>
          </div>
        </div>
        <h2 class="mt-6 text-center text-3xl font-extrabold text-gray-900">
          Breyta lykilorði
        </h2>
        <p class="mt-2 text-center text-sm text-gray-600">
          Þú verður að breyta lykilorðinu áður en þú getur haldið áfram
        </p>
      </div>
      
      <form class="mt-8 space-y-6" @submit.prevent="handleChangePassword">
        <div v-if="errorMsg" class="rounded-md bg-red-50 p-4">
          <div class="flex">
            <div class="flex-shrink-0">
              <svg class="h-5 w-5 text-red-400" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
              </svg>
            </div>
            <div class="ml-3">
              <p class="text-sm font-medium text-red-800">{{ errorMsg }}</p>
            </div>
          </div>
        </div>

        <div class="space-y-4">
          <div>
            <label for="currentPassword" class="block text-sm font-medium text-gray-700">
              Núverandi lykilorð
            </label>
            <input
              id="currentPassword"
              v-model="currentPassword"
              name="currentPassword"
              type="password"
              required
              class="mt-1 appearance-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
              placeholder="Núverandi lykilorð"
            />
          </div>
          <div>
            <label for="newPassword" class="block text-sm font-medium text-gray-700">
              Nýtt lykilorð
            </label>
            <input
              id="newPassword"
              v-model="newPassword"
              name="newPassword"
              type="password"
              required
              minlength="4"
              class="mt-1 appearance-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
              placeholder="Nýtt lykilorð"
            />
            <p class="mt-1 text-xs text-gray-500">Lykilorð verður að vera að minnsta kosti 4 stafir</p>
          </div>
          <div>
            <label for="confirmPassword" class="block text-sm font-medium text-gray-700">
              Staðfesta nýtt lykilorð
            </label>
            <input
              id="confirmPassword"
              v-model="confirmPassword"
              name="confirmPassword"
              type="password"
              required
              class="mt-1 appearance-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
              placeholder="Staðfesta nýtt lykilorð"
            />
          </div>
        </div>

        <div v-if="passwordMismatch" class="rounded-md bg-yellow-50 p-4">
          <div class="flex">
            <div class="flex-shrink-0">
              <svg class="h-5 w-5 text-yellow-400" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clip-rule="evenodd" />
              </svg>
            </div>
            <div class="ml-3">
              <p class="text-sm font-medium text-yellow-800">Lykilorðin passa ekki saman</p>
            </div>
          </div>
        </div>

        <div>
          <button
            type="submit"
            :disabled="isLoading || passwordMismatch"
            class="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            <span v-if="!isLoading">Breyta lykilorði</span>
            <span v-else class="flex items-center">
              <svg class="animate-spin -ml-1 mr-3 h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              Breyti lykilorði...
            </span>
          </button>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup lang="ts">
definePageMeta({
  layout: false,
  middleware: []
})

const { changePassword, getCurrentUser } = useAuth()
const currentPassword = ref('')
const newPassword = ref('')
const confirmPassword = ref('')
const errorMsg = ref('')
const isLoading = ref(false)

const passwordMismatch = computed(() => {
  return newPassword.value && confirmPassword.value && newPassword.value !== confirmPassword.value
})

// Ensure user is authenticated
onMounted(async () => {
  const user = await getCurrentUser()
  if (!user) {
    navigateTo('/login')
  } else if (!user.mustChangePassword) {
    navigateTo('/')
  }
})

const handleChangePassword = async () => {
  if (passwordMismatch.value) {
    return
  }

  errorMsg.value = ''
  isLoading.value = true

  try {
    await changePassword(currentPassword.value, newPassword.value)
    // After successful password change, redirect to home
    navigateTo('/')
  } catch (error: any) {
    errorMsg.value = error.data?.message || error.message || 'Mistókst að breyta lykilorði. Vinsamlegast reyndu aftur.'
  } finally {
    isLoading.value = false
  }
}
</script>

