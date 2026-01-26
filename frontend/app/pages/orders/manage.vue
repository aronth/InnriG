<template>
  <div class="min-h-[80vh] space-y-6">
    <!-- Header -->
    <div class="bg-gradient-to-br from-indigo-50 via-purple-50 to-pink-50 rounded-2xl p-8 border border-indigo-100 shadow-lg">
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-3">
          <div class="w-12 h-12 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-xl flex items-center justify-center shadow-lg">
            <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
            </svg>
          </div>
          <div>
            <h1 class="text-2xl font-bold text-gray-800">Stjórna pöntunum</h1>
            <p class="text-sm text-gray-600">Breyta pöntunum og skoða upplýsingar</p>
          </div>
        </div>
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
            @click="loadOrders"
            :disabled="isLoading"
            class="w-full px-4 py-2 bg-gradient-to-r from-indigo-500 to-purple-600 text-white rounded-lg hover:from-indigo-600 hover:to-purple-700 transition-all duration-200 shadow-md hover:shadow-lg font-medium disabled:opacity-50"
          >
            {{ isLoading ? 'Hleður...' : 'Sækja' }}
          </button>
        </div>
      </div>

      <div v-if="paginatedData" class="mt-4 text-sm text-gray-600">
        Sýnir {{ paginatedData.items.length }} af {{ paginatedData.totalCount.toLocaleString('is-IS') }} pöntunum (síða {{ paginatedData.page }} af {{ paginatedData.totalPages }})
      </div>
    </div>

    <!-- Subnav -->
    <OrderSubNav />

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
    <div v-else-if="paginatedData && paginatedData.items.length > 0" class="bg-white rounded-2xl shadow-lg border border-gray-100 overflow-hidden">
      <div class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200">
          <thead class="bg-gradient-to-r from-indigo-50 to-purple-50">
            <tr>
              <th class="px-6 py-4 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">Pöntunarnúmer</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">Nafn/Heimilisfang</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">Veitingastaður</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">Afgreiðsluaðferð</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">Afhendingardagur</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">Tilbúið kl.</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">Skannað kl.</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">Skráð út kl.</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">Staða</th>
              <th class="px-6 py-4 text-center text-xs font-medium text-gray-700 uppercase tracking-wider">Aðgerðir</th>
            </tr>
          </thead>
          <tbody class="bg-white divide-y divide-gray-200">
            <tr v-for="order in paginatedData.items" :key="order.id" class="hover:bg-indigo-50 transition-colors duration-150">
              <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                {{ order.orderNumber || '-' }}
              </td>
              <td class="px-6 py-4 text-sm text-gray-900">
                <div class="max-w-xs">
                  <div v-if="order.description" class="font-medium">{{ order.description }}</div>
                  <div v-else class="text-gray-400">-</div>
                </div>
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                {{ getRestaurantName(order.restaurantId) }}
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">{{ order.deliveryMethod }}</td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                {{ formatDate(order.deliveryDate) }}
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                <div class="flex items-center gap-2">
                  <input
                    v-if="editingReadyTime === order.id"
                    v-model="editingReadyTimeValue"
                    type="time"
                    class="px-2 py-1 border border-gray-300 rounded text-sm"
                    @blur="saveReadyTime(order.id)"
                    @keyup.enter="saveReadyTime(order.id)"
                    @keyup.esc="cancelEdit"
                  />
                  <span v-else class="cursor-pointer hover:text-indigo-600" @click="startEditReadyTime(order)">
                    {{ formatTime(order.readyTime) }}
                  </span>
                </div>
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                {{ formatTimeFromDateTime(order.scannedAt) }}
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                {{ formatTimeFromDateTime(order.checkedOutAt) }}
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-center">
                <span
                  v-if="isOrderLate(order)"
                  class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-800"
                >
                  Sein
                </span>
                <span
                  v-else-if="isOrderEvaluable(order)"
                  class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800"
                >
                  Ekki sein
                </span>
                <span v-else class="text-xs text-gray-400">-</span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-center">
                <div class="flex items-center justify-center gap-2">
                  <button
                    v-if="isWebOrder(order)"
                    @click="viewOrder(order)"
                    class="inline-flex items-center px-3 py-1.5 text-sm font-medium text-indigo-600 hover:text-indigo-800 hover:bg-indigo-50 rounded-lg transition-colors duration-200"
                    title="Skoða pöntun"
                  >
                    <svg class="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                    </svg>
                    Skoða
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Pagination -->
      <div class="flex justify-between items-center p-4 border-t border-gray-200">
        <button
          @click="prevPage"
          :disabled="!paginatedData || paginatedData.page === 1 || isLoading"
          class="px-4 py-2 bg-gray-200 text-gray-700 rounded-lg hover:bg-gray-300 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          Fyrri síða
        </button>
        <span class="text-sm text-gray-700">
          Síða {{ paginatedData?.page || 0 }} af {{ paginatedData?.totalPages || 0 }}
        </span>
        <button
          @click="nextPage"
          :disabled="!paginatedData || paginatedData.page >= paginatedData.totalPages || isLoading"
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
      <h3 class="text-xl font-bold text-gray-700 mb-2">Engar pantanir fundust</h3>
      <p class="text-gray-500">Engar pantanir fundust með völdum síum.</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { OrderRow, PaginatedOrderRows } from '../../composables/useOrders'

const { getRowsPaginated, updateReadyTime } = useOrders()
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
const pageSize = 100

// State
const isLoading = ref(false)
const error = ref<string | null>(null)
const paginatedData = ref<PaginatedOrderRows | null>(null)

// Edit state
const editingReadyTime = ref<string | null>(null)
const editingReadyTimeValue = ref('')

// Helper functions
const formatDate = (dateString?: string | null) => {
  if (!dateString) return '-'
  try {
    return new Date(dateString).toLocaleDateString('is-IS')
  } catch {
    return dateString
  }
}

const formatTime = (timeString?: string | null) => {
  if (!timeString) return '-'
  try {
    // TimeOnly format is "HH:mm:ss" or "HH:mm"
    const parts = timeString.split(':')
    if (parts.length >= 2) {
      return `${parts[0].padStart(2, '0')}:${parts[1].padStart(2, '0')}`
    }
    return timeString
  } catch {
    return timeString
  }
}

const formatTimeFromDateTime = (dateString?: string | null) => {
  if (!dateString) return '-'
  try {
    const date = new Date(dateString)
    return date.toLocaleTimeString('is-IS', { 
      hour: '2-digit', 
      minute: '2-digit' 
    })
  } catch {
    return dateString
  }
}

const getRestaurantName = (restaurantId?: string | null) => {
  if (!restaurantId) return '-'
  const restaurant = restaurants.value.find(r => r.id === restaurantId)
  return restaurant?.name || '-'
}

const isWebOrder = (order: OrderRow): boolean => {
  // Web orders have OrderSource = "Web" and OrderNumber is typically 6 digits
  if (order.orderSource !== 'Web') return false
  if (!order.orderNumber) return false
  // Extract digits from order number
  const digits = order.orderNumber.replace(/\D/g, '')
  // Web orders are typically 6 digits (>= 100000) but can be >= 1000
  return digits.length >= 4 && parseInt(digits) >= 1000
}

const getOrderUrl = (order: OrderRow): string | null => {
  if (!isWebOrder(order) || !order.orderNumber) return null
  // Extract digits from order number
  const digits = order.orderNumber.replace(/\D/g, '')
  if (!digits) return null
  // Format: https://api.spretturinn.is/is/moya/pilot/order/print/{orderId}
  return `https://api.spretturinn.is/is/moya/pilot/order/print/${digits}`
}

const viewOrder = (order: OrderRow) => {
  const url = getOrderUrl(order)
  if (url) {
    window.open(url, '_blank')
  }
}

const isOrderLate = (order: OrderRow): boolean => {
  if (!order.deliveryDate) return false
  
  // Combine DeliveryDate with ReadyTime to get the actual ready datetime
  let readyDateTime: Date
  if (order.readyTime) {
    const deliveryDate = new Date(order.deliveryDate)
    const readyTimeParts = order.readyTime.split(':')
    if (readyTimeParts.length >= 2) {
      const readyHours = parseInt(readyTimeParts[0], 10)
      const readyMinutes = parseInt(readyTimeParts[1], 10)
      readyDateTime = new Date(deliveryDate)
      readyDateTime.setHours(readyHours, readyMinutes, 0, 0)
    } else {
      readyDateTime = new Date(order.deliveryDate)
    }
  } else {
    readyDateTime = new Date(order.deliveryDate)
  }
  
  if (order.deliveryMethod === 'Sent') {
    if (!order.checkedOutAt) return false
    const checkedOut = new Date(order.checkedOutAt)
    return (readyDateTime.getTime() - checkedOut.getTime()) / (1000 * 60) < 15
  } else {
    if (!order.scannedAt) return false
    const scanned = new Date(order.scannedAt)
    return (readyDateTime.getTime() - scanned.getTime()) / (1000 * 60) < 7
  }
}

const isOrderEvaluable = (order: OrderRow): boolean => {
  if (order.deliveryMethod === 'Sent') {
    return order.checkedOutAt != null
  } else {
    return order.scannedAt != null
  }
}

const startEditReadyTime = (order: OrderRow) => {
  editingReadyTime.value = order.id
  if (order.readyTime) {
    // Convert "HH:mm:ss" or "HH:mm" to "HH:mm" format for time input
    const parts = order.readyTime.split(':')
    editingReadyTimeValue.value = `${parts[0].padStart(2, '0')}:${parts[1]?.padStart(2, '0') || '00'}`
  } else {
    editingReadyTimeValue.value = ''
  }
}

const cancelEdit = () => {
  editingReadyTime.value = null
  editingReadyTimeValue.value = ''
}

const saveReadyTime = async (orderId: string) => {
  if (editingReadyTime.value !== orderId) return

  const readyTimeValue = editingReadyTimeValue.value || null
  isLoading.value = true
  error.value = null

  try {
    await updateReadyTime(orderId, readyTimeValue)
    // Reload orders to get updated data
    await loadOrders()
    editingReadyTime.value = null
    editingReadyTimeValue.value = ''
  } catch (e: any) {
    error.value = e?.data?.message || e?.message || 'Villa kom upp við að uppfæra tíma.'
  } finally {
    isLoading.value = false
  }
}

const loadRestaurants = async () => {
  try {
    restaurants.value = await apiFetch<Restaurant[]>(`${apiBase}/api/restaurants`)
  } catch (e: any) {
    console.error('Failed to load restaurants:', e)
  }
}

const loadOrders = async () => {
  isLoading.value = true
  error.value = null

  try {
    const skip = (currentPage.value - 1) * pageSize
    paginatedData.value = await getRowsPaginated({
      restaurantId: restaurantId.value || undefined,
      fromDate: fromDate.value,
      toDate: toDate.value,
      deliveryMethod: deliveryMethod.value || undefined,
      isLate: lateFilter.value ?? undefined,
      skip,
      take: pageSize
    })
  } catch (e: any) {
    error.value = e?.data?.message || e?.message || 'Villa kom upp við að sækja pantanir.'
  } finally {
    isLoading.value = false
  }
}

const nextPage = () => {
  if (paginatedData && paginatedData.value && paginatedData.value.page < paginatedData.value.totalPages) {
    currentPage.value++
    loadOrders()
  }
}

const prevPage = () => {
  if (currentPage.value > 1) {
    currentPage.value--
    loadOrders()
  }
}

onMounted(async () => {
  await loadRestaurants()
  await loadOrders()
})

watch([restaurantId, fromDate, toDate, deliveryMethod, lateFilter], () => {
  currentPage.value = 1
  loadOrders()
})
</script>

