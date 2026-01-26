<template>
  <div class="min-h-[80vh] space-y-6">
    <!-- Header -->
    <div class="bg-gradient-to-br from-indigo-50 via-purple-50 to-pink-50 rounded-2xl p-8 border border-indigo-100 shadow-lg">
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-3">
          <div class="w-12 h-12 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-xl flex items-center justify-center shadow-lg">
            <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M9 17v-2a4 4 0 00-4-4H3m18 0h-2a4 4 0 00-4 4v2m-6 4h6m-6 0a2 2 0 01-2-2v-1m8 3a2 2 0 002-2v-1m-8 0h8M7 7a4 4 0 118 0 4 4 0 01-8 0z"
              />
            </svg>
          </div>
          <div>
            <h1 class="text-2xl font-bold text-gray-800">Pantanir</h1>
            <p class="text-sm text-gray-600">{{ totalOrderCount.toLocaleString('is-IS') }} pantanir í kerfinu</p>
          </div>
        </div>

        <button
          @click="refresh"
          :disabled="isLoading"
          class="inline-flex items-center px-4 py-2 bg-white border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors text-sm font-medium text-gray-700 disabled:opacity-50"
        >
          <svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v6h6M20 20v-6h-6M5 11a7 7 0 0112.9-3M19 13a7 7 0 01-12.9 3" />
          </svg>
          Endurhlaða
        </button>
      </div>
    </div>

    <!-- Subnav -->
    <OrderSubNav />

    <!-- Stats Cards -->
    <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
      <div class="bg-white rounded-2xl shadow-lg border border-gray-100 p-6">
        <div class="flex items-center justify-between">
          <div>
            <p class="text-sm font-medium text-gray-600">Síðasta pöntun</p>
            <p class="text-2xl font-bold text-gray-900 mt-1">
              {{ lastOrderDate ? formatDate(lastOrderDate) : '-' }}
            </p>
          </div>
          <div class="w-12 h-12 bg-indigo-100 rounded-lg flex items-center justify-center">
            <svg class="w-6 h-6 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
            </svg>
          </div>
        </div>
      </div>

      <div class="bg-white rounded-2xl shadow-lg border border-gray-100 p-6">
        <div class="flex items-center justify-between">
          <div>
            <p class="text-sm font-medium text-gray-600">Síðasti innlestur</p>
            <p class="text-2xl font-bold text-gray-900 mt-1">
              {{ lastImportDate ? formatDate(lastImportDate) : '-' }}
            </p>
          </div>
          <div class="w-12 h-12 bg-purple-100 rounded-lg flex items-center justify-center">
            <svg class="w-6 h-6 text-purple-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
            </svg>
          </div>
        </div>
      </div>

      <div class="bg-white rounded-2xl shadow-lg border border-gray-100 p-6">
        <div class="flex items-center justify-between">
          <div>
            <p class="text-sm font-medium text-gray-600">Heildarfjöldi pantana</p>
            <p class="text-2xl font-bold text-gray-900 mt-1">{{ totalOrderCount.toLocaleString('is-IS') }}</p>
          </div>
          <div class="w-12 h-12 bg-pink-100 rounded-lg flex items-center justify-center">
            <svg class="w-6 h-6 text-pink-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
            </svg>
          </div>
        </div>
      </div>
    </div>

    <!-- KPIs Section -->
    <div v-if="summaryData" class="bg-white rounded-2xl shadow-lg border border-gray-100 p-6">
      <h2 class="text-lg font-bold text-gray-800 mb-4">Yfirlit úr öllum gögnum</h2>
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <!-- Late Ratio -->
        <div class="bg-gradient-to-br from-red-50 to-red-100 rounded-xl p-4">
          <p class="text-sm text-gray-600 mb-1">Hlutfall seina</p>
          <p class="text-2xl font-bold text-gray-900">{{ formatPercent(summaryData.lateRatio) }}</p>
          <p class="text-xs text-gray-500 mt-1">{{ summaryData.lateOrders }} af {{ summaryData.evaluableOrders }} mælanlegum</p>
        </div>

        <!-- Average Wait Time -->
        <div class="bg-gradient-to-br from-yellow-50 to-yellow-100 rounded-xl p-4">
          <p class="text-sm text-gray-600 mb-1">Meðal biðtími</p>
          <p class="text-2xl font-bold text-gray-900">
            {{ summaryData.avgWaitTimeMin ? `${Math.round(summaryData.avgWaitTimeMin)} mín` : '-' }}
          </p>
          <p class="text-xs text-gray-500 mt-1" v-if="summaryData.p90WaitTimeMin">
            P90: {{ Math.round(summaryData.p90WaitTimeMin) }} mín
          </p>
        </div>

        <!-- Total Revenue -->
        <div class="bg-gradient-to-br from-green-50 to-green-100 rounded-xl p-4">
          <p class="text-sm text-gray-600 mb-1">Heildartekjur</p>
          <p class="text-2xl font-bold text-gray-900">{{ formatCurrency(summaryData.totalRevenue) }}</p>
          <p class="text-xs text-gray-500 mt-1" v-if="summaryData.averageOrderValue > 0">
            Meðal: {{ formatCurrency(summaryData.averageOrderValue) }}
          </p>
        </div>

        <!-- Total Orders -->
        <div class="bg-gradient-to-br from-blue-50 to-blue-100 rounded-xl p-4">
          <p class="text-sm text-gray-600 mb-1">Samtals pantanir</p>
          <p class="text-2xl font-bold text-gray-900">{{ summaryData.totalOrders.toLocaleString('is-IS') }}</p>
          <p class="text-xs text-gray-500 mt-1">{{ summaryData.evaluableOrders }} mælanlegar</p>
        </div>
      </div>

      <!-- Delivery Method Breakdown -->
      <div class="mt-6">
        <h3 class="text-md font-semibold text-gray-800 mb-3">Dreifing eftir afgreiðslu</h3>
        <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div 
            v-for="(count, method) in summaryData.ordersByMethod" 
            :key="method"
            class="bg-gray-50 rounded-lg p-4 border border-gray-200"
          >
            <div class="flex items-center justify-between mb-2">
              <span class="text-sm font-medium text-gray-700">{{ method || 'Óþekkt' }}</span>
              <span class="text-lg font-bold text-gray-900">{{ count.toLocaleString('is-IS') }}</span>
            </div>
            <div class="w-full bg-gray-200 rounded-full h-2">
              <div 
                class="bg-indigo-600 h-2 rounded-full transition-all"
                :style="{ width: `${(count / summaryData.totalOrders) * 100}%` }"
              ></div>
            </div>
            <p class="text-xs text-gray-500 mt-1">{{ formatPercent(count / summaryData.totalOrders) }}</p>
          </div>
        </div>
      </div>

      <!-- Order Source Breakdown -->
      <div class="mt-6">
        <h3 class="text-md font-semibold text-gray-800 mb-3">Dreifing eftir pöntunarmáta</h3>
        <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div 
            v-for="(count, source) in summaryData.ordersBySource" 
            :key="source"
            class="bg-gray-50 rounded-lg p-4 border border-gray-200"
          >
            <div class="flex items-center justify-between mb-2">
              <span class="text-sm font-medium text-gray-700">{{ getOrderSourceLabel(source) }}</span>
              <span class="text-lg font-bold text-gray-900">{{ count.toLocaleString('is-IS') }}</span>
            </div>
            <div class="w-full bg-gray-200 rounded-full h-2">
              <div 
                class="bg-purple-600 h-2 rounded-full transition-all"
                :style="{ width: `${(count / summaryData.totalOrders) * 100}%` }"
              ></div>
            </div>
            <p class="text-xs text-gray-500 mt-1">{{ formatPercent(count / summaryData.totalOrders) }}</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Recent Batches Overview -->
    <div class="bg-white rounded-2xl shadow-lg border border-gray-100 overflow-hidden">
      <div class="p-6 border-b border-gray-100 flex items-center justify-between">
        <h2 class="text-lg font-bold text-gray-800">Nýlegir innlestrar</h2>
        <p class="text-sm text-gray-500">Nýjasta fyrst</p>
      </div>

      <div v-if="isLoading" class="p-10 flex justify-center">
        <div class="relative">
          <div class="w-16 h-16 border-4 border-indigo-200 rounded-full"></div>
          <div class="w-16 h-16 border-4 border-indigo-600 border-t-transparent rounded-full animate-spin absolute top-0"></div>
        </div>
      </div>

      <div v-else-if="loadError" class="p-6">
        <div class="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-800">
          {{ loadError }}
        </div>
      </div>

      <div v-else-if="!batches || batches.length === 0" class="p-10 text-center text-gray-600">
        Engar pantanir hafa verið lesnar inn ennþá.
      </div>

      <div v-else-if="batches && batches.length > 0" class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200">
          <thead class="bg-gradient-to-r from-indigo-50 to-purple-50">
            <tr>
              <th class="px-6 py-4 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">Skrá</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">Innflutt</th>
              <th class="px-6 py-4 text-right text-xs font-medium text-gray-700 uppercase tracking-wider">Raðir</th>
              <th class="px-6 py-4 text-center text-xs font-medium text-gray-700 uppercase tracking-wider">Aðgerðir</th>
            </tr>
          </thead>
          <tbody class="bg-white divide-y divide-gray-200">
            <tr v-for="b in batches" :key="b.id" class="hover:bg-indigo-50 transition-colors duration-150">
              <td class="px-6 py-4 whitespace-nowrap">
                <span class="text-sm font-medium text-gray-900">{{ b.fileName }}</span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap">
                <span class="text-sm text-gray-700">{{ formatDateTime(b.importedAt) }}</span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-right">
                <span class="text-sm font-bold text-gray-900">{{ b.rowCount.toLocaleString('is-IS') }}</span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-center">
                <div class="flex items-center justify-center gap-2">
                  <NuxtLink
                    :to="`/orders/${b.id}`"
                    class="inline-flex items-center px-3 py-1.5 text-sm font-medium text-indigo-600 hover:text-indigo-800 hover:bg-indigo-50 rounded-lg transition-colors duration-200"
                  >
                    Skoða
                  </NuxtLink>
                  <button
                    @click="confirmDelete(b)"
                    class="inline-flex items-center px-3 py-1.5 text-sm font-medium text-red-600 hover:text-red-800 hover:bg-red-50 rounded-lg transition-colors duration-200"
                    title="Eyða innlestri"
                  >
                    Eyða
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Delete Confirmation Modal -->
    <div v-if="showDeleteModal && deletingBatch" class="fixed z-50 inset-0 overflow-y-auto" @click.self="showDeleteModal = false">
      <div class="flex items-center justify-center min-h-screen pt-4 px-4 pb-20 text-center sm:block sm:p-0">
        <div class="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity" @click="showDeleteModal = false"></div>
        <div class="inline-block align-bottom bg-white rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-lg sm:w-full">
          <div class="bg-white px-4 pt-5 pb-4 sm:p-6 sm:pb-4">
            <div class="sm:flex sm:items-start">
              <div class="mx-auto flex-shrink-0 flex items-center justify-center h-12 w-12 rounded-full bg-red-100 sm:mx-0 sm:h-10 sm:w-10">
                <svg class="h-6 w-6 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                </svg>
              </div>
              <div class="mt-3 text-center sm:mt-0 sm:ml-4 sm:text-left">
                <h3 class="text-lg leading-6 font-medium text-gray-900">Eyða innlestri</h3>
                <div class="mt-2">
                  <p class="text-sm text-gray-500">
                    Ertu viss um að þú viljir eyða innlestri <strong>{{ deletingBatch.fileName }}</strong>?
                    Þetta eyðir einnig öllum pöntunarröðum sem tilheyra innlestrinum og er óafturkræft.
                  </p>
                </div>
              </div>
            </div>
          </div>
          <div class="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse">
            <button
              @click="handleDelete"
              :disabled="isDeleting"
              class="w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-red-600 text-base font-medium text-white hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500 sm:ml-3 sm:w-auto sm:text-sm disabled:opacity-50"
            >
              {{ isDeleting ? 'Eyði...' : 'Eyða' }}
            </button>
            <button
              @click="showDeleteModal = false"
              class="mt-3 w-full inline-flex justify-center rounded-md border border-gray-300 shadow-sm px-4 py-2 bg-white text-base font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 sm:mt-0 sm:ml-3 sm:w-auto sm:text-sm"
            >
              Hætta við
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { OrderReportSummaryDto } from '~/app/composables/useOrders'

const { getBatches, deleteBatch, getRows, getCompleteSummary } = useOrders()
const config = useRuntimeConfig()
const apiBase = config.public.apiBase
const { apiFetch } = useApi()

type OrderImportBatch = {
  id: string
  fileName: string
  importedAt: string
  rowCount: number
}

const isLoading = ref(false)
const loadError = ref<string | null>(null)
const batches = ref<OrderImportBatch[]>([])
const totalOrderCount = ref(0)
const lastOrderDate = ref<string | null>(null)
const lastImportDate = ref<string | null>(null)
const summaryData = ref<OrderReportSummaryDto | null>(null)

const showDeleteModal = ref(false)
const deletingBatch = ref<OrderImportBatch | null>(null)
const isDeleting = ref(false)

const formatDate = (value?: string | null) => {
  if (!value) return '-'
  try {
    return new Date(value).toLocaleDateString('is-IS', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit'
    })
  } catch {
    return value
  }
}

const formatDateTime = (value?: string | null) => {
  if (!value) return '-'
  try {
    return new Date(value).toLocaleString('is-IS')
  } catch {
    return value
  }
}

const formatPercent = (value: number) => {
  return new Intl.NumberFormat('is-IS', { style: 'percent', maximumFractionDigits: 1 }).format(value)
}

const formatCurrency = (value: number) => {
  return new Intl.NumberFormat('is-IS', { 
    style: 'currency', 
    currency: 'ISK',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0
  }).format(value)
}

const getOrderSourceLabel = (source: string | null | undefined): string => {
  if (!source) return 'Óþekkt'
  switch (source) {
    case 'Counter':
      return 'Kassi'
    case 'Web':
      return 'Vefur'
    case 'Unknown':
      return 'Óþekkt'
    default:
      return source
  }
}

const refresh = async () => {
  isLoading.value = true
  loadError.value = null
  try {
    batches.value = await getBatches()
    
    // Calculate stats from batches
    if (batches.value.length > 0) {
      lastImportDate.value = batches.value[0].importedAt
      totalOrderCount.value = batches.value.reduce((sum, b) => sum + b.rowCount, 0)
    }
    
    // Get last order date from most recent order row
    try {
      const rows = await getRows({ take: 1 })
      if (rows.length > 0 && rows[0].deliveryDate) {
        lastOrderDate.value = rows[0].deliveryDate
      } else if (rows.length > 0 && rows[0].orderDate) {
        lastOrderDate.value = rows[0].orderDate
      } else if (rows.length > 0 && rows[0].createdDate) {
        lastOrderDate.value = rows[0].createdDate
      }
    } catch (e) {
      // If we can't get last order date, that's okay
      console.warn('Could not fetch last order date:', e)
    }

    // Load complete summary data for KPIs (all data, no date filter)
    try {
      summaryData.value = await getCompleteSummary()
    } catch (e) {
      console.warn('Could not fetch complete summary data:', e)
    }
  } catch (e: any) {
    loadError.value = e?.data?.message || e?.message || 'Villa kom upp við að sækja gögn.'
  } finally {
    isLoading.value = false
  }
}

const confirmDelete = (batch: OrderImportBatch) => {
  deletingBatch.value = batch
  showDeleteModal.value = true
}

const handleDelete = async () => {
  if (!deletingBatch.value) return

  isDeleting.value = true
  try {
    await deleteBatch(deletingBatch.value.id)
    showDeleteModal.value = false
    deletingBatch.value = null
    await refresh()
  } catch (e: any) {
    loadError.value = e?.data?.message || e?.message || 'Villa kom upp við að eyða innlestri.'
  } finally {
    isDeleting.value = false
  }
}

onMounted(async () => {
  await refresh()
})
</script>
