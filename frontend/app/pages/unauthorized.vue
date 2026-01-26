<template>
  <div class="min-h-[70vh] flex items-center justify-center px-4">
    <div class="max-w-md w-full text-center">
      <div class="mb-8">
        <div class="mx-auto h-24 w-24 bg-red-100 rounded-full flex items-center justify-center mb-4">
          <svg class="h-12 w-12 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
          </svg>
        </div>
        <h1 class="text-3xl font-bold text-gray-900 mb-2">Ekki aðgangur</h1>
        <p class="text-lg text-gray-600 mb-4">
          Þú hefur ekki heimild til að skoða þessa síðu.
        </p>
        <p class="text-sm text-gray-500">
          Ef þú telur að þú ættir að hafa aðgang, hafðu samband við stjórnanda.
        </p>
      </div>
      
      <div v-if="currentUser" class="mb-6 p-4 bg-gray-50 rounded-lg">
        <p class="text-sm text-gray-600 mb-1">Þú ert skráð/ur inn sem:</p>
        <p class="font-medium text-gray-900">{{ currentUser.name }} ({{ currentUser.username }})</p>
        <p v-if="currentUser.roles && currentUser.roles.length > 0" class="text-sm text-indigo-600 mt-1">
          Hlutverk: {{ currentUser.roles.join(', ') }}
        </p>
      </div>
      
      <div class="flex gap-3 justify-center">
        <button
          @click="$router.back()"
          class="inline-flex items-center px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 transition-colors"
        >
          <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
          </svg>
          Til baka
        </button>
        <NuxtLink
          to="/"
          class="inline-flex items-center px-4 py-2 text-sm font-medium text-white bg-indigo-600 rounded-md hover:bg-indigo-700 transition-colors"
        >
          <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
          </svg>
          Fara heim
        </NuxtLink>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
const { currentUser } = useAuth()
</script>

