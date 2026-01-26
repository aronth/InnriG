<template>
  <div class="min-h-[80vh] space-y-6">
    <!-- Header -->
    <div class="bg-gradient-to-br from-indigo-50 via-purple-50 to-pink-50 rounded-2xl p-8 border border-indigo-100 shadow-lg">
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-3">
          <div class="w-12 h-12 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-xl flex items-center justify-center shadow-lg">
            <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
            </svg>
          </div>
          <div>
            <h1 class="text-2xl font-bold text-gray-800">Eftirstandandi Biðtími</h1>
            <p class="text-sm text-gray-600">Meðaltal eftirstandandi biðtíma eftir 15 mínútna tímabilum</p>
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
      <div class="mt-6 grid grid-cols-1 md:grid-cols-5 gap-4">
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
          <label class="block text-sm font-medium text-gray-700 mb-1">Frá</label>
          <input v-model="fromDate" type="date" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent" />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Til</label>
          <input v-model="toDate" type="date" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent" />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Afgreiðsluaðferð</label>
          <select v-model="deliveryMethod" class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent">
            <option value="">Allar</option>
            <option value="Sótt">Sótt</option>
            <option value="Sent">Sent</option>
          </select>
        </div>
        <div class="flex items-end">
          <button
            @click="loadData"
            :disabled="isLoading"
            class="w-full px-4 py-2 bg-gradient-to-r from-indigo-500 to-purple-600 text-white rounded-lg hover:from-indigo-600 hover:to-purple-700 transition-all duration-200 shadow-md hover:shadow-lg font-medium disabled:opacity-50"
          >
            {{ isLoading ? 'Hleður...' : 'Endurhlaða' }}
          </button>
        </div>
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

    <!-- Charts -->
    <div v-else-if="aggregations && aggregations.length > 0" class="space-y-8">
      <div
        v-for="dayData in aggregations"
        :key="dayData.date"
        class="bg-white rounded-2xl shadow-lg border border-gray-100 overflow-hidden"
      >
        <div class="p-6 border-b border-gray-100">
          <h2 class="text-lg font-bold text-gray-800">{{ dayData.dateLabel }}</h2>
          <p class="text-sm text-gray-600">Meðaltal eftirstandandi biðtími fyrir pantanir í hverju 15 mínútna tímabili</p>
        </div>
        <div class="p-6">
          <div class="h-96">
            <Bar v-if="getChartData(dayData)" :data="getChartData(dayData)" :options="getChartOptions(dayData)" />
          </div>
        </div>
      </div>
    </div>

    <!-- No Data -->
    <div v-else class="bg-white rounded-2xl shadow-lg border border-gray-100 p-10 text-center text-gray-600">
      Engin gögn fundust fyrir valið tímabil.
    </div>
  </div>
</template>

<script setup lang="ts">
import { Bar } from 'vue-chartjs'
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  BarElement,
  Title,
  Tooltip,
  Legend
} from 'chart.js'

ChartJS.register(CategoryScale, LinearScale, BarElement, Title, Tooltip, Legend)

import type { RemainingTimeAggregationDto, RemainingTimeIntervalData } from '../../composables/useOrders'
import { useRemainingTime } from '../../composables/useOrders'

const { getRemainingTimeAggregations } = useRemainingTime()
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
const sevenDaysAgo = new Date(today.getTime() - 7 * 24 * 60 * 60 * 1000)
const fromDate = ref(sevenDaysAgo.toISOString().split('T')[0])
const toDate = ref(today.toISOString().split('T')[0])
const deliveryMethod = ref('')
const restaurants = ref<Restaurant[]>([])
const restaurantId = ref<string | null>(null)

// State
const isLoading = ref(false)
const error = ref<string | null>(null)
const aggregations = ref<RemainingTimeAggregationDto[]>([])

const loadRestaurants = async () => {
  try {
    restaurants.value = await apiFetch<Restaurant[]>(`${apiBase}/api/restaurants`)
  } catch (e: any) {
    console.error('Failed to load restaurants:', e)
  }
}

const loadData = async () => {
  isLoading.value = true
  error.value = null
  try {
    aggregations.value = await getRemainingTimeAggregations({
      restaurantId: restaurantId.value || undefined,
      deliveryMethod: deliveryMethod.value || undefined,
      fromDate: fromDate.value || undefined,
      toDate: toDate.value || undefined
    })
  } catch (e: any) {
    error.value = e?.data?.message || e?.message || 'Villa kom upp við að sækja gögn.'
  } finally {
    isLoading.value = false
  }
}

const getChartData = (dayData: RemainingTimeAggregationDto) => {
  // Filter to business hours (6:00 - 23:45) and only show intervals with data
  // This makes the chart more readable
  const businessHoursIntervals = dayData.intervals.filter((i: RemainingTimeIntervalData) => {
    const timeParts = i.timeLabel.split(':')
    if (timeParts.length === 0 || !timeParts[0]) return false
    const hour = parseInt(timeParts[0])
    return !isNaN(hour) && hour >= 6 && hour <= 23
  })
  
  // Only show intervals that have data (orderCount > 0) or are adjacent to intervals with data
  // This reduces clutter while maintaining context
  const intervalsWithData = businessHoursIntervals.filter((i: RemainingTimeIntervalData, idx: number) => {
    if (i.orderCount > 0) return true
    // Include if adjacent interval has data
    const prevInterval = idx > 0 ? businessHoursIntervals[idx - 1] : undefined
    const nextInterval = idx < businessHoursIntervals.length - 1 ? businessHoursIntervals[idx + 1] : undefined
    if (prevInterval && prevInterval.orderCount > 0) return true
    if (nextInterval && nextInterval.orderCount > 0) return true
    return false
  })
  
  const labels = intervalsWithData.map((i: RemainingTimeIntervalData) => i.timeLabel)
  const data = intervalsWithData.map((i: RemainingTimeIntervalData) => i.averageRemainingMinutes ?? 0)

  // Use a single dataset - Chart.js will automatically show negative values below 0
  return {
    labels,
    datasets: [
      {
        label: 'Eftirstandandi tími (mín)',
        data: data,
        backgroundColor: data.map((v: number) => {
          if (v === null || v === 0) return 'rgba(156, 163, 175, 0.3)'
          return v > 0 ? 'rgba(34, 197, 94, 0.6)' : 'rgba(239, 68, 68, 0.6)'
        }),
        borderColor: data.map((v: number) => {
          if (v === null || v === 0) return 'rgba(156, 163, 175, 0.5)'
          return v > 0 ? 'rgba(34, 197, 94, 1)' : 'rgba(239, 68, 68, 1)'
        }),
        borderWidth: 1
      }
    ]
  }
}

const getChartOptions = (dayData: RemainingTimeAggregationDto) => {
  const allValues = dayData.intervals
    .map((i: RemainingTimeIntervalData) => i.averageRemainingMinutes)
    .filter((v: number | null): v is number => v !== null)
  
  const maxAbs = allValues.length > 0 
    ? Math.max(...allValues.map(Math.abs)) 
    : 30
  
  const yMax = Math.ceil(maxAbs / 10) * 10 // Round up to nearest 10

  return {
    responsive: true,
    maintainAspectRatio: false,
    indexAxis: 'x' as const,
    scales: {
      x: {
        ticks: {
          maxRotation: 90,
          minRotation: 45,
          font: {
            size: 9
          },
          maxTicksLimit: 48 // Show at most 48 labels (every 30 minutes if we have 96 intervals)
        },
        grid: {
          display: false
        }
      },
      y: {
        beginAtZero: true,
        min: -yMax,
        max: yMax,
        ticks: {
          callback: function(value: string | number) {
            return typeof value === 'number' ? value + ' mín' : value
          }
        },
        grid: {
          color: function(context: any) {
            if (context.tick.value === 0) {
              return 'rgba(0, 0, 0, 0.3)'
            }
            return 'rgba(0, 0, 0, 0.1)'
          },
          lineWidth: function(context: any) {
            if (context.tick.value === 0) {
              return 2
            }
            return 1
          }
        }
      }
    },
    plugins: {
      legend: {
        display: true,
        position: 'top' as const
      },
      title: {
        display: false
      },
      tooltip: {
        callbacks: {
          label: function(context: any) {
            const value = context.parsed.y
            const index = context.dataIndex
            const orderCount = dayData.intervals[index]?.orderCount || 0
            const originalValue = dayData.intervals[index]?.averageRemainingMinutes
            
            if (originalValue === null) {
              return orderCount > 0 
                ? `Meðaltal: ${value.toFixed(1)} mín (${orderCount} pantanir)`
                : 'Engin gögn'
            }
            
            const sign = value > 0 ? '+' : ''
            return `Meðaltal: ${sign}${value.toFixed(1)} mín (${orderCount} pantanir)`
          }
        }
      }
    }
  }
}

// Watch for date changes and auto-adjust if invalid
watch(fromDate, (newFrom) => {
  if (newFrom && toDate.value && newFrom > toDate.value) {
    toDate.value = newFrom
  }
})

watch(toDate, (newTo) => {
  if (newTo && fromDate.value && newTo < fromDate.value) {
    fromDate.value = newTo
  }
})

onMounted(async () => {
  await Promise.all([loadRestaurants(), loadData()])
})

watch([restaurantId, fromDate, toDate, deliveryMethod], () => {
  loadData()
})
</script>

