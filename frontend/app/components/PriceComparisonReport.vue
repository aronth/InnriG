<template>
  <div class="bg-white rounded-lg shadow-md p-6">
    <h2 class="text-2xl font-bold mb-4">Verðbreytingaskýrsla</h2>
    
    <!-- Date Selection -->
    <div class="grid grid-cols-2 gap-4 mb-6">
      <div>
        <label class="block text-sm font-medium text-gray-700 mb-2">
          Frá dagsetningu
        </label>
        <input
          v-model="fromDate"
          type="date"
          class="w-full px-3 py-2 border border-gray-300 rounded-md"
        />
      </div>
      <div>
        <label class="block text-sm font-medium text-gray-700 mb-2">
          Til dagsetningar
        </label>
        <input
          v-model="toDate"
          type="date"
          class="w-full px-3 py-2 border border-gray-300 rounded-md"
        />
      </div>
    </div>

    <div class="flex gap-3 mb-6">
      <button
        @click="loadComparison"
        :disabled="loading || !fromDate || !toDate"
        class="px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed"
      >
        Sækja skýrslu
      </button>
      <button
        v-if="products.length > 0"
        @click="handleExport"
        :disabled="loading || !fromDate || !toDate"
        class="px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed"
      >
        Sækja CSV
      </button>
    </div>

    <!-- Summary -->
    <div v-if="summary" class="mt-6 grid grid-cols-4 gap-4 mb-6">
      <div class="bg-blue-50 p-4 rounded-lg">
        <div class="text-sm text-gray-600">Heildarvörur</div>
        <div class="text-2xl font-bold">{{ summary.totalProducts }}</div>
      </div>
      <div class="bg-green-50 p-4 rounded-lg">
        <div class="text-sm text-gray-600">Verðhækkanir</div>
        <div class="text-2xl font-bold">{{ summary.productsWithPriceIncrease }}</div>
      </div>
      <div class="bg-red-50 p-4 rounded-lg">
        <div class="text-sm text-gray-600">Verðlækkanir</div>
        <div class="text-2xl font-bold">{{ summary.productsWithPriceDecrease }}</div>
      </div>
      <div class="bg-gray-50 p-4 rounded-lg">
        <div class="text-sm text-gray-600">Meðalbreyting</div>
        <div class="text-2xl font-bold">
          {{ formatPercent(summary.averagePriceChangePercent) }}
        </div>
      </div>
    </div>

    <!-- Error -->
    <div v-if="error" class="text-red-600 text-center py-4">
      {{ error }}
    </div>

    <!-- Loading -->
    <div v-if="loading" class="text-center py-8">
      <div class="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600"></div>
    </div>

    <!-- Product List -->
    <div v-else-if="products.length > 0" class="mt-6">
      <table class="min-w-full divide-y divide-gray-200">
        <thead class="bg-gray-50">
          <tr>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Vara</th>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Birgir</th>
            <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Frá verði</th>
            <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Til verðs</th>
            <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Breyting</th>
            <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">%</th>
          </tr>
        </thead>
        <tbody class="bg-white divide-y divide-gray-200">
          <tr v-for="product in products" :key="product.productId" class="hover:bg-gray-50">
            <td class="px-4 py-3">
              <div class="font-medium">{{ product.productName }}</div>
              <div class="text-sm text-gray-500">{{ product.productCode }}</div>
            </td>
            <td class="px-4 py-3 text-sm">{{ product.supplierName }}</td>
            <td class="px-4 py-3 text-right">
              {{ formatCurrency(product.fromUnitPrice) }}
            </td>
            <td class="px-4 py-3 text-right">
              {{ formatCurrency(product.toUnitPrice) }}
            </td>
            <td class="px-4 py-3 text-right">
              <span :class="getChangeClass(product.unitPriceChange)">
                {{ formatCurrency(product.unitPriceChange) }}
              </span>
            </td>
            <td class="px-4 py-3 text-right">
              <span :class="getChangeClass(product.unitPriceChange)">
                {{ formatPercent(product.unitPriceChangePercent) }}
              </span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-else-if="!loading && fromDate && toDate" class="text-gray-500 text-center py-8">
      Engar vörur fundust með verð á báðum dagsetningunum
    </div>
  </div>
</template>

<script setup lang="ts">
import type { PriceComparisonDto, PriceComparisonSummaryDto } from '~/app/composables/usePriceComparison'

const { getPriceComparison, exportToCsv } = usePriceComparison()

const fromDate = ref('')
const toDate = ref('')
const products = ref<PriceComparisonDto[]>([])
const summary = ref<PriceComparisonSummaryDto | null>(null)
const loading = ref(false)
const error = ref<string | null>(null)

// Set default dates (3 months ago to today)
onMounted(() => {
  const today = new Date()
  const threeMonthsAgo = new Date()
  threeMonthsAgo.setMonth(today.getMonth() - 3)
  
  toDate.value = today.toISOString().split('T')[0]
  fromDate.value = threeMonthsAgo.toISOString().split('T')[0]
})

const loadComparison = async () => {
  if (!fromDate.value || !toDate.value) return
  
  try {
    loading.value = true
    error.value = null
    const result = await getPriceComparison(fromDate.value, toDate.value)
    products.value = result.products
    summary.value = result.summary
  } catch (e: any) {
    error.value = e.message || 'Villa við að sækja skýrslu'
    console.error('Error loading price comparison:', e)
  } finally {
    loading.value = false
  }
}

const getChangeClass = (change: number) => {
  if (change > 0) return 'text-green-600 font-semibold'
  if (change < 0) return 'text-red-600 font-semibold'
  return 'text-gray-600'
}

const formatCurrency = (value: number) => {
  return new Intl.NumberFormat('is-IS', {
    style: 'currency',
    currency: 'ISK',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }).format(value)
}

const formatPercent = (value: number) => {
  const sign = value >= 0 ? '+' : ''
  return `${sign}${new Intl.NumberFormat('is-IS', {
    minimumFractionDigits: 1,
    maximumFractionDigits: 1
  }).format(value)}%`
}

const handleExport = async () => {
  if (!fromDate.value || !toDate.value) return
  
  try {
    await exportToCsv(fromDate.value, toDate.value)
  } catch (e: any) {
    error.value = e.message || 'Villa við að sækja CSV'
    console.error('Error exporting CSV:', e)
  }
}
</script>

