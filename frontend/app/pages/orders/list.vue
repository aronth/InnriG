<template>
  <div class="min-h-[80vh] space-y-6">
    <!-- Header -->
    <div class="bg-gradient-to-br from-indigo-50 via-purple-50 to-pink-50 rounded-2xl p-8 border border-indigo-100 shadow-lg">
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-3">
          <div class="w-12 h-12 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-xl flex items-center justify-center shadow-lg">
            <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-3 7h3m-3 4h3m-6-4h.01M9 16h.01" />
            </svg>
          </div>
          <div>
            <h1 class="text-2xl font-bold text-gray-800">Pöntunarraðir</h1>
            <p class="text-sm text-gray-600">Skoða allar pöntunarraðir með síum</p>
          </div>
        </div>

        <NuxtLink
          to="/orders"
          class="inline-flex items-center px-4 py-2 bg-white border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors text-sm font-medium text-gray-700"
        >
          <svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
          </svg>
          Til baka
        </NuxtLink>
      </div>

      <!-- Filters -->
      <div class="mt-6 grid grid-cols-1 md:grid-cols-6 gap-4">
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Veitingastaður</label>
          <select v-model="restaurantId" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent">
            <option :value="null">Allir</option>
            <option v-for="restaurant in restaurants" :key="restaurant.id" :value="restaurant.id">
              {{ restaurant.name }}
            </option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Frá dagsetningu</label>
          <input v-model="fromDate" type="date" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent" />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Til dagsetningar</label>
          <input v-model="toDate" type="date" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent" />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Afgreiðsluaðferð</label>
          <select v-model="deliveryMethod" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent">
            <option value="">Allar</option>
            <option value="Sótt">Sótt</option>
            <option value="Sent">Sent</option>
            <option value="Salur">Salur</option>
            <option value="Unknown">Óþekkt</option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Staða</label>
          <select v-model="lateFilter" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent">
            <option :value="null">Allar</option>
            <option :value="true">Seinar</option>
            <option :value="false">Ekki seinar</option>
          </select>
        </div>
        <div class="flex items-end">
          <button
            @click="loadRows"
            :disabled="isLoading"
            class="w-full px-4 py-2 bg-gradient-to-r from-indigo-500 to-purple-600 text-white rounded-lg hover:from-indigo-600 hover:to-purple-700 transition-all duration-200 shadow-md hover:shadow-lg font-medium disabled:opacity-50"
          >
            {{ isLoading ? 'Hleður...' : 'Sækja' }}
          </button>
        </div>
      </div>

      <div class="mt-4 text-sm text-gray-600">
        Sýnir {{ rows.length }} raðir (síða {{ currentPage }})
      </div>
    </div>

    <!-- Loading -->
    <div v-if="isLoading" class="p-10 flex justify-center">
      <div class="relative">
        <div class="w-16 h-16 border-4 border-indigo-200 rounded-full"></div>
        <div class="w-16 h-16 border-4 border-indigo-600 border-t-transparent rounded-full animate-spin absolute top-0"></div>
      </div>
    </div>

    <!-- Error -->
    <div v-else-if="error" class="bg-red-50 border border-red-200 rounded-2xl p-6 text-red-800">
      <p class="font-bold">Villa kom upp</p>
      <p class="text-sm mt-1">{{ error }}</p>
    </div>

    <!-- Table -->
    <div v-else-if="rows.length > 0" class="bg-white rounded-2xl shadow-lg border border-gray-100 overflow-hidden">
      <div class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200">
          <thead class="bg-gradient-to-r from-indigo-50 to-purple-50">
            <tr>
              <th class="px-6 py-4 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">Afgreiðsluaðferð</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">Pöntunardagur</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">Afhendingartími</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">Skannað kl.</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">Skráð út kl.</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">Eftirstandandi tími</th>
              <th class="px-6 py-4 text-center text-xs font-medium text-gray-700 uppercase tracking-wider">Sein</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">Reikningstexti 3</th>
            </tr>
          </thead>
          <tbody class="bg-white divide-y divide-gray-200">
            <tr v-for="row in rows" :key="row.id" class="hover:bg-indigo-50 transition-colors duration-150">
              <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">{{ row.deliveryMethod }}</td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">{{ formatDate(row.createdDate) }}</td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">{{ formatDateTime(row.deliveryDate) }}</td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{{ formatDateTime(row.scannedAt) }}</td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{{ formatDateTime(row.checkedOutAt) }}</td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                <span v-if="getRemainingTime(row) !== null" class="font-medium">
                  {{ formatRemainingTime(getRemainingTime(row)) }}
                </span>
                <span v-else class="text-gray-400">-</span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-center">
                <span
                  v-if="isRowLate(row)"
                  class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-800"
                >
                  Sein
                </span>
                <span
                  v-else-if="isRowEvaluable(row)"
                  class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800"
                >
                  Ekki sein
                </span>
                <span v-else class="text-xs text-gray-400">-</span>
              </td>
              <td class="px-6 py-4 text-sm text-gray-600 max-w-md truncate" :title="row.invoiceText3Raw || ''">
                {{ row.invoiceText3Raw || '-' }}
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Pagination -->
      <div class="flex justify-between items-center p-4 border-t border-gray-200">
        <button
          @click="prevPage"
          :disabled="currentPage === 1 || isLoading"
          class="px-4 py-2 bg-gray-200 text-gray-700 rounded-lg hover:bg-gray-300 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          Fyrri síða
        </button>
        <span class="text-sm text-gray-700">Síða {{ currentPage }}</span>
        <button
          @click="nextPage"
          :disabled="rows.length < pageSize || isLoading"
          class="px-4 py-2 bg-gray-200 text-gray-700 rounded-lg hover:bg-gray-300 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          Næsta síða
        </button>
      </div>
    </div>

    <!-- Empty State -->
    <div v-else class="bg-gradient-to-br from-gray-50 to-gray-100 rounded-2xl p-12 text-center border-2 border-dashed border-gray-300">
      <div class="w-20 h-20 bg-gray-200 rounded-full flex items-center justify-center mx-auto mb-4">
        <svg class="w-10 h-10 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
        </svg>
      </div>
      <h3 class="text-xl font-bold text-gray-700 mb-2">Engar raðir fundust</h3>
      <p class="text-gray-500">Engar pöntunarraðir fundust með völdum síum.</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { OrderRow } from '../../composables/useOrders'

const { getRows } = useOrders()
const config = useRuntimeConfig()
const apiBase = config.public.apiBase
const { apiFetch } = useApi()

type Restaurant = {
  id: string
  name: string
  code: string
}

// Filters
const today = new Date()
const thirtyDaysAgo = new Date(today.getTime() - 30 * 24 * 60 * 60 * 1000)
const fromDate = ref(thirtyDaysAgo.toISOString().split('T')[0])
const toDate = ref(today.toISOString().split('T')[0])
const deliveryMethod = ref('')
const lateFilter = ref<boolean | null>(null)

const restaurants = ref<Restaurant[]>([])
const restaurantId = ref<string | null>(null)

// Pagination
const currentPage = ref(1)
const pageSize = 200

// State
const isLoading = ref(false)
const error = ref<string | null>(null)
const rows = ref<OrderRow[]>([])

// Helper to check if a row is late (client-side for display)
const isRowLate = (row: OrderRow): boolean => {
  if (!row.deliveryDate) return false
  const deliveryDate = new Date(row.deliveryDate)
  if (row.deliveryMethod === 'Sent') {
    if (!row.checkedOutAt) return false
    const checkedOut = new Date(row.checkedOutAt)
    return (deliveryDate.getTime() - checkedOut.getTime()) / (1000 * 60) < 15
  } else {
    if (!row.scannedAt) return false
    const scanned = new Date(row.scannedAt)
    return (deliveryDate.getTime() - scanned.getTime()) / (1000 * 60) < 7
  }
}

const isRowEvaluable = (row: OrderRow): boolean => {
  if (row.deliveryMethod === 'Sent') {
    return row.checkedOutAt != null
  } else {
    return row.scannedAt != null
  }
}

const formatDate = (dateString?: string | null) => {
  if (!dateString) return '-'
  try {
    return new Date(dateString).toLocaleDateString('is-IS')
  } catch {
    return dateString
  }
}

const formatDateTime = (dateString?: string | null) => {
  if (!dateString) return '-'
  try {
    return new Date(dateString).toLocaleString('is-IS', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
  } catch {
    return dateString
  }
}

const getRemainingTime = (row: OrderRow): number | null => {
  // Not available for "Salur" orders
  if (row.deliveryMethod === 'Salur') {
    return null
  }

  // Need ReadyTime and deliveryDate to calculate
  if (!row.readyTime || !row.deliveryDate) {
    return null
  }

  // Parse ReadyTime (format: "HH:mm:ss" or "HH:mm")
  const readyTimeParts = row.readyTime.split(':')
  if (readyTimeParts.length < 2) {
    return null
  }

  const readyHours = parseInt(readyTimeParts[0], 10)
  const readyMinutes = parseInt(readyTimeParts[1], 10)

  if (isNaN(readyHours) || isNaN(readyMinutes)) {
    return null
  }

  // Create ReadyTime DateTime by combining deliveryDate with ReadyTime
  const deliveryDate = new Date(row.deliveryDate)
  const readyDateTime = new Date(deliveryDate)
  readyDateTime.setHours(readyHours, readyMinutes, 0, 0)

  // Determine which timestamp to use based on delivery method
  let actionDateTime: Date | null = null

  if (row.deliveryMethod === 'Sótt') {
    // For "Sótt" orders, use ScannedAt (CheckedOutAt is never available)
    if (row.scannedAt) {
      actionDateTime = new Date(row.scannedAt)
    }
  } else if (row.deliveryMethod === 'Sent') {
    // For "Sent" orders, use CheckedOutAt
    if (row.checkedOutAt) {
      actionDateTime = new Date(row.checkedOutAt)
    }
  }

  if (!actionDateTime) {
    return null
  }

  // Calculate difference in minutes
  const diffMs = readyDateTime.getTime() - actionDateTime.getTime()
  const diffMinutes = Math.round(diffMs / (1000 * 60))

  return diffMinutes
}

const formatRemainingTime = (minutes: number | null): string => {
  if (minutes === null) return '-'

  const absMinutes = Math.abs(minutes)
  const hours = Math.floor(absMinutes / 60)
  const mins = absMinutes % 60

  if (hours > 0) {
    return `${minutes < 0 ? '-' : ''}${hours}kl ${mins} mín`
  } else {
    return `${minutes < 0 ? '-' : ''}${mins} mín`
  }
}

const loadRestaurants = async () => {
  try {
    restaurants.value = await apiFetch<Restaurant[]>(`${apiBase}/api/restaurants`)
    // Set Spretturinn as default
    const spretturinn = restaurants.value.find(r => r.code === 'SPR')
    if (spretturinn) {
      restaurantId.value = spretturinn.id
    }
  } catch (e: any) {
    console.error('Failed to load restaurants:', e)
  }
}

const loadRows = async () => {
  isLoading.value = true
  error.value = null

  try {
    const skip = (currentPage.value - 1) * pageSize
    rows.value = await getRows({
      restaurantId: restaurantId.value || undefined,
      fromDate: fromDate.value,
      toDate: toDate.value,
      deliveryMethod: deliveryMethod.value || undefined,
      isLate: lateFilter.value ?? undefined,
      skip,
      take: pageSize
    })
  } catch (e: any) {
    error.value = e?.data?.message || e?.message || 'Villa kom upp við að sækja raðir.'
  } finally {
    isLoading.value = false
  }
}

const nextPage = () => {
  if (rows.value.length === pageSize) {
    currentPage.value++
    loadRows()
  }
}

const prevPage = () => {
  if (currentPage.value > 1) {
    currentPage.value--
    loadRows()
  }
}

onMounted(async () => {
  await loadRestaurants()
  await loadRows()
})

watch(restaurantId, () => {
  currentPage.value = 1
  loadRows()
})
</script>

