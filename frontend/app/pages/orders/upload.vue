<template>
  <div class="min-h-[80vh] space-y-6">
    <!-- Header -->
    <div class="bg-gradient-to-br from-indigo-50 via-purple-50 to-pink-50 rounded-2xl p-8 border border-indigo-100 shadow-lg">
      <div class="flex items-center gap-3">
        <div class="w-12 h-12 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-xl flex items-center justify-center shadow-lg">
          <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
          </svg>
        </div>
        <div>
          <h1 class="text-2xl font-bold text-gray-800">Hlaða upp pantanir</h1>
          <p class="text-sm text-gray-600">Lesa inn pantanir úr Excel skrá</p>
        </div>
      </div>
    </div>

    <!-- Subnav -->
    <OrderSubNav />

    <!-- Upload Section -->
    <div class="bg-white rounded-2xl shadow-lg border border-gray-100 overflow-hidden">
      <div class="p-6 border-b border-gray-100">
        <h2 class="text-lg font-bold text-gray-800">Lesa inn pantanir (Excel)</h2>
        <p class="text-sm text-gray-600">Hlaðið upp Excel skrá (sheet: <span class="font-mono">data</span>).</p>
      </div>

      <div class="p-6">
        <div class="mb-4">
          <label class="block text-sm font-medium text-gray-700 mb-2">
            Veitingastaður
          </label>
          <select
            v-model="selectedRestaurantId"
            class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
          >
            <option :value="null">Veldu veitingastað...</option>
            <option v-for="restaurant in restaurants" :key="restaurant.id" :value="restaurant.id">
              {{ restaurant.name }}
            </option>
          </select>
        </div>
        <ExcelDropzone :disabled="!selectedRestaurantId" @file-selected="onFileSelected" />

        <div v-if="uploadError" class="mt-4 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-800">
          {{ uploadError }}
        </div>

        <div v-if="lastResult" class="mt-6 rounded-2xl border border-green-200 bg-green-50 p-6">
          <div class="flex items-center justify-between gap-4">
            <div>
              <h3 class="text-lg font-bold text-green-900">Innlestri lokið</h3>
              <p class="text-sm text-green-800">
                {{ lastResult.fileName }} — {{ formatDateTime(lastResult.importedAt) }}
              </p>
            </div>
            <NuxtLink
              :to="`/orders/${lastResult.batchId}`"
              class="inline-flex items-center px-4 py-2 bg-gradient-to-r from-indigo-500 to-purple-600 text-white rounded-lg hover:from-indigo-600 hover:to-purple-700 transition-all duration-200 shadow-md hover:shadow-lg font-medium"
            >
              Skoða raðir
            </NuxtLink>
          </div>

          <div class="mt-4 grid grid-cols-1 sm:grid-cols-3 gap-4 text-sm">
            <div class="rounded-xl bg-white/70 border border-green-100 p-4">
              <p class="text-gray-500">Raðir í skjali</p>
              <p class="text-xl font-bold text-gray-900">{{ lastResult.totalRowsInSheet }}</p>
            </div>
            <div class="rounded-xl bg-white/70 border border-green-100 p-4">
              <p class="text-gray-500">Flutt inn</p>
              <p class="text-xl font-bold text-gray-900">{{ lastResult.importedRows }}</p>
            </div>
            <div class="rounded-xl bg-white/70 border border-green-100 p-4">
              <p class="text-gray-500">Sleppt</p>
              <p class="text-xl font-bold text-gray-900">{{ lastResult.skippedRows }}</p>
            </div>
          </div>

          <div v-if="lastResult.warnings?.length" class="mt-4">
            <p class="text-sm font-semibold text-green-900 mb-2">Viðvaranir</p>
            <ul class="list-disc list-inside space-y-1 text-xs text-green-900">
              <li v-for="(w, idx) in lastResult.warnings" :key="idx">{{ w }}</li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
const { uploadOrders } = useOrders()
const config = useRuntimeConfig()
const apiBase = config.public.apiBase
const { apiFetch } = useApi()

type OrderImportResultDto = {
  batchId: string
  fileName: string
  importedAt: string
  totalRowsInSheet: number
  importedRows: number
  skippedRows: number
  warnings: string[]
}

type Restaurant = {
  id: string
  name: string
  code: string
}

const uploadError = ref<string | null>(null)
const lastResult = ref<OrderImportResultDto | null>(null)
const restaurants = ref<Restaurant[]>([])
const selectedRestaurantId = ref<string | null>(null)
const isLoading = ref(false)

const formatDateTime = (value?: string | null) => {
  if (!value) return '-'
  try {
    return new Date(value).toLocaleString('is-IS')
  } catch {
    return value
  }
}

const loadRestaurants = async () => {
  try {
    restaurants.value = await apiFetch<Restaurant[]>(`${apiBase}/api/restaurants`)
  } catch (e: any) {
    console.error('Failed to load restaurants:', e)
  }
}

const onFileSelected = async (file: File) => {
  if (!selectedRestaurantId.value) {
    uploadError.value = 'Vinsamlegast veldu veitingastað fyrst.'
    return
  }

  uploadError.value = null
  lastResult.value = null

  isLoading.value = true
  try {
    lastResult.value = await uploadOrders(file, selectedRestaurantId.value)
  } catch (e: any) {
    uploadError.value = e?.data || e?.message || 'Villa kom upp við að hlaða upp skjali.'
  } finally {
    isLoading.value = false
  }
}

onMounted(async () => {
  await loadRestaurants()
})
</script>

