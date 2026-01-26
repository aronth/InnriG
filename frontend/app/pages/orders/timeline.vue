<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="bg-gradient-to-r from-indigo-600 to-indigo-800 rounded-lg p-6 text-white shadow-md">
      <h1 class="text-2xl font-bold mb-1">Tímalína pantana</h1>
      <p class="text-indigo-100">
        Skoða tímamælingar fyrir pantanir á einum degi
      </p>
    </div>

    <!-- Controls Card -->
    <div class="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <!-- Date Picker with Prev/Next -->
        <div class="flex items-center gap-2">
          <button
            @click="previousDay"
            class="p-2 text-gray-600 hover:text-indigo-600 hover:bg-indigo-50 rounded-lg transition-colors"
            title="Fyrri dagur"
          >
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
            </svg>
          </button>
          <input
            v-model="selectedDate"
            type="date"
            class="flex-1 rounded-lg border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500"
            @change="loadData"
          />
          <button
            @click="nextDay"
            class="p-2 text-gray-600 hover:text-indigo-600 hover:bg-indigo-50 rounded-lg transition-colors"
            title="Næsti dagur"
          >
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
            </svg>
          </button>
        </div>

        <!-- Location Filter -->
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Staðsetning</label>
          <select
            v-model="selectedRestaurantId"
            class="w-full rounded-lg border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500"
            @change="loadData"
          >
            <option :value="null">Allar staðsetningar</option>
            <option v-for="restaurant in restaurants" :key="restaurant.id" :value="restaurant.id">
              {{ restaurant.name }}
            </option>
          </select>
        </div>

        <!-- Delivery Method Filter -->
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Afhendingaraðferð</label>
          <select
            v-model="selectedDeliveryMethod"
            class="w-full rounded-lg border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500"
            @change="loadData"
          >
            <option :value="null">Allar aðferðir</option>
            <option value="Sótt">Sótt</option>
            <option value="Sent">Sent</option>
            <option value="Salur">Salur</option>
          </select>
        </div>

        <!-- Summary Stats -->
        <div class="flex flex-col justify-center">
          <div class="text-sm text-gray-600">Fjöldi pantana</div>
          <div class="text-2xl font-bold text-indigo-600">{{ timelineData?.orders.length || 0 }}</div>
        </div>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="bg-white rounded-lg shadow-sm border border-gray-200 p-12 text-center">
      <div class="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600"></div>
      <p class="mt-4 text-gray-600">Hleð gögnum...</p>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="bg-red-50 border border-red-200 rounded-lg p-4 text-red-700">
      {{ error }}
    </div>

    <!-- Chart -->
    <div v-else-if="timelineData" class="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
      <h2 class="text-lg font-semibold text-gray-900 mb-4">Tímamælingar</h2>
      
      <!-- No data message -->
      <div v-if="timelineData.orders.length === 0" class="text-center py-12 text-gray-500">
        Engar pantanir fundust fyrir valinn dag
      </div>
      
      <!-- Chart -->
      <div v-else>
        <OrderTimelineChart :timeline-data="timelineData" />
      </div>
    </div>

    <!-- Data Table -->
    <div v-if="timelineData && timelineData.orders.length > 0" class="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
      <div class="p-6 border-b border-gray-200">
        <h2 class="text-lg font-semibold text-gray-900">Ítarlegar upplýsingar</h2>
      </div>
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead class="bg-gray-50 border-b border-gray-200">
            <tr>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Pöntunartími
              </th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Pöntunarnúmer
              </th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Afhending
              </th>
              <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                Biðtími
              </th>
              <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                Skönnun
              </th>
              <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                Útskráning
              </th>
              <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                Upphæð
              </th>
            </tr>
          </thead>
          <tbody class="bg-white divide-y divide-gray-200">
            <tr v-for="order in timelineData.orders" :key="order.orderId" class="hover:bg-gray-50">
              <td class="px-6 py-4 whitespace-nowrap text-gray-900">
                {{ formatTime(order.orderTime) }}
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-gray-600">
                {{ order.orderNumber || '-' }}
              </td>
              <td class="px-6 py-4 whitespace-nowrap">
                <span :class="getDeliveryMethodClass(order.deliveryMethod)">
                  {{ order.deliveryMethod }}
                </span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-right text-gray-900">
                {{ order.waitTimeMinutes != null ? `${order.waitTimeMinutes} mín` : '-' }}
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-right text-gray-900">
                {{ order.timeToScanMinutes != null ? `${Math.round(order.timeToScanMinutes)} mín` : '-' }}
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-right text-gray-900">
                {{ order.timeToCheckoutMinutes != null ? `${Math.round(order.timeToCheckoutMinutes)} mín` : '-' }}
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-right text-gray-900">
                {{ order.totalAmount != null ? formatCurrency(order.totalAmount) : '-' }}
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { OrderTimelineReportDto } from '~/composables/useOrders'

const { getTimeline } = useOrders()
const { getRestaurants } = useRestaurants()

// State
const selectedDate = ref<string>(new Date().toISOString().split('T')[0])
const selectedRestaurantId = ref<string | null>(null)
const selectedDeliveryMethod = ref<string | null>(null)
const timelineData = ref<OrderTimelineReportDto | null>(null)
const restaurants = ref<any[]>([])
const loading = ref(false)
const error = ref<string | null>(null)

// Load restaurants on mount
onMounted(async () => {
  try {
    restaurants.value = await getRestaurants()
  } catch (err) {
    console.error('Failed to load restaurants:', err)
  }
  await loadData()
})

// Load timeline data
const loadData = async () => {
  loading.value = true
  error.value = null
  
  try {
    timelineData.value = await getTimeline({
      date: selectedDate.value,
      deliveryMethod: selectedDeliveryMethod.value || undefined,
      restaurantId: selectedRestaurantId.value || undefined
    })
  } catch (err: any) {
    error.value = err.message || 'Villa við að sækja gögn'
    console.error('Failed to load timeline:', err)
  } finally {
    loading.value = false
  }
}

// Date navigation
const previousDay = () => {
  const date = new Date(selectedDate.value)
  date.setDate(date.getDate() - 1)
  selectedDate.value = date.toISOString().split('T')[0]
  loadData()
}

const nextDay = () => {
  const date = new Date(selectedDate.value)
  date.setDate(date.getDate() + 1)
  selectedDate.value = date.toISOString().split('T')[0]
  loadData()
}

// Formatting helpers
const formatTime = (dateString: string): string => {
  const date = new Date(dateString)
  return date.toLocaleTimeString('is-IS', { hour: '2-digit', minute: '2-digit' })
}

const formatCurrency = (amount: number): string => {
  return new Intl.NumberFormat('is-IS', {
    style: 'currency',
    currency: 'ISK',
    minimumFractionDigits: 0
  }).format(amount)
}

const getDeliveryMethodClass = (method: string): string => {
  const classes: Record<string, string> = {
    'Sótt': 'px-2 py-1 text-xs font-medium rounded-full bg-blue-100 text-blue-800',
    'Sent': 'px-2 py-1 text-xs font-medium rounded-full bg-green-100 text-green-800',
    'Salur': 'px-2 py-1 text-xs font-medium rounded-full bg-purple-100 text-purple-800'
  }
  return classes[method] || 'px-2 py-1 text-xs font-medium rounded-full bg-gray-100 text-gray-800'
}
</script>

