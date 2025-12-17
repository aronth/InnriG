<template>
  <div class="min-h-[80vh]">
    <!-- Header -->
    <div class="bg-gradient-to-br from-indigo-50 via-purple-50 to-pink-50 rounded-2xl p-8 border border-indigo-100 shadow-lg mb-6">
      <div class="flex items-center gap-3 mb-4">
        <div class="w-12 h-12 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-xl flex items-center justify-center shadow-lg">
          <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
          </svg>
        </div>
        <div>
          <h1 class="text-2xl font-bold text-gray-800">Mest pantaðar vörur</h1>
          <p class="text-sm text-gray-600">Skoðaðu vörur eftir magni sem pantað hefur verið á tímabilinu</p>
        </div>
      </div>
      
      <!-- Date Selection and Filters -->
      <div class="mt-6 grid grid-cols-1 md:grid-cols-3 gap-4">
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-2">
            Frá dagsetningu
          </label>
          <input
            v-model="fromDate"
            type="date"
            class="w-full px-4 py-2 border-2 border-indigo-200 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition-all"
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-2">
            Til dagsetningar
          </label>
          <input
            v-model="toDate"
            type="date"
            class="w-full px-4 py-2 border-2 border-indigo-200 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition-all"
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-2">
            Birgir (valfrjálst)
          </label>
          <select
            v-model="selectedSupplierId"
            class="w-full px-4 py-2 border-2 border-indigo-200 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition-all"
          >
            <option value="">Allir birgir</option>
            <option v-for="supplier in suppliers" :key="supplier.id" :value="supplier.id">
              {{ supplier.name }}
            </option>
          </select>
        </div>
      </div>

      <div class="mt-4 flex gap-3">
        <button
          @click="loadMostOrdered"
          :disabled="loading || !fromDate || !toDate"
          class="px-6 py-2 bg-gradient-to-r from-indigo-500 to-purple-600 text-white rounded-lg hover:from-indigo-600 hover:to-purple-700 transition-all duration-200 shadow-md hover:shadow-lg font-medium disabled:opacity-50 disabled:cursor-not-allowed"
        >
          <span v-if="loading">Hleður...</span>
          <span v-else>Sækja skýrslu</span>
        </button>
        <button
          v-if="products.length > 0"
          @click="handleExport"
          :disabled="loading || !fromDate || !toDate"
          class="px-6 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-all duration-200 shadow-md hover:shadow-lg font-medium disabled:opacity-50 disabled:cursor-not-allowed"
        >
          Sækja CSV
        </button>
        <button
          v-if="products.length > 0"
          @click="resetFilters"
          class="px-6 py-2 bg-gray-200 text-gray-700 rounded-lg hover:bg-gray-300 transition-all duration-200 font-medium"
        >
          Hreinsa síur
        </button>
      </div>
    </div>

    <!-- Error Message -->
    <div v-if="error" class="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg text-red-700">
      {{ error }}
    </div>

    <!-- Results Table -->
    <div v-if="products.length > 0" class="bg-white rounded-2xl shadow-lg border-2 border-gray-100 overflow-hidden">
      <div class="p-6 border-b border-gray-200">
        <h2 class="text-xl font-bold text-gray-800">
          Niðurstöður ({{ products.length }} vörur)
        </h2>
        <p class="text-sm text-gray-600 mt-1">
          Sýnir {{ products.length }} mest pantaðu vörurnar eftir magni á tímabilinu
        </p>
      </div>
      
      <div class="overflow-x-auto">
        <table class="w-full">
          <thead class="bg-gradient-to-r from-indigo-50 to-purple-50">
            <tr>
              <th class="px-6 py-4 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">
                #
              </th>
              <th class="px-6 py-4 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">
                Vörunúmer
              </th>
              <th class="px-6 py-4 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">
                Vöruheiti
              </th>
              <th class="px-6 py-4 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">
                Birgir
              </th>
              <th class="px-6 py-4 text-right text-xs font-semibold text-gray-700 uppercase tracking-wider">
                Heildarmagn
              </th>
              <th class="px-6 py-4 text-right text-xs font-semibold text-gray-700 uppercase tracking-wider">
                Fjöldi pantana
              </th>
              <th class="px-6 py-4 text-right text-xs font-semibold text-gray-700 uppercase tracking-wider">
                Meðalverð
              </th>
              <th class="px-6 py-4 text-right text-xs font-semibold text-gray-700 uppercase tracking-wider">
                Síðasta verð
              </th>
              <th class="px-6 py-4 text-left text-xs font-semibold text-gray-700 uppercase tracking-wider">
                Eining
              </th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-200">
            <tr
              v-for="(product, index) in products"
              :key="product.productId"
              class="hover:bg-indigo-50 transition-colors cursor-pointer"
              @click="navigateToProduct(product.productId)"
            >
              <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                {{ index + 1 }}
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm font-mono text-gray-700">
                {{ product.productCode }}
              </td>
              <td class="px-6 py-4 text-sm text-gray-900">
                {{ product.productName }}
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-600">
                {{ product.supplierName }}
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm font-semibold text-indigo-600 text-right">
                {{ formatQuantity(product.totalQuantity) }}
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-600 text-right">
                {{ product.orderCount }}
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-700 text-right">
                <span v-if="product.averageUnitPrice">
                  {{ formatCurrency(product.averageUnitPrice) }}
                </span>
                <span v-else class="text-gray-400">-</span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-700 text-right">
                <span v-if="product.latestUnitPrice">
                  {{ formatCurrency(product.latestUnitPrice) }}
                </span>
                <span v-else class="text-gray-400">-</span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                {{ product.unit || '-' }}
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Empty State -->
    <div v-else-if="!loading && fromDate && toDate" class="bg-white rounded-2xl shadow-lg border-2 border-gray-100 p-12 text-center">
      <div class="text-gray-400 mb-4">
        <svg class="w-16 h-16 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4" />
        </svg>
      </div>
      <p class="text-gray-500 text-lg">Engar vörur fundust á þessu tímabili</p>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="bg-white rounded-2xl shadow-lg border-2 border-gray-100 p-12 text-center">
      <div class="inline-block animate-spin rounded-full h-12 w-12 border-4 border-indigo-500 border-t-transparent"></div>
      <p class="mt-4 text-gray-600">Hleður gögnum...</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { MostOrderedProductDto } from '~/app/composables/useProducts'

const { getMostOrderedProducts, exportMostOrderedToCsv } = useProducts()
const { getAllSuppliers } = useSuppliers()

const fromDate = ref('')
const toDate = ref('')
const selectedSupplierId = ref('')
const products = ref<MostOrderedProductDto[]>([])
const suppliers = ref<Array<{ id: string; name: string }>>([])
const loading = ref(false)
const error = ref<string | null>(null)

// Load suppliers on mount
onMounted(async () => {
  try {
    const supplierList = await getAllSuppliers()
    suppliers.value = supplierList.map(s => ({ id: s.id, name: s.name }))
  } catch (e) {
    console.error('Error loading suppliers:', e)
  }

  // Set default dates (last 3 months)
  const today = new Date()
  const threeMonthsAgo = new Date()
  threeMonthsAgo.setMonth(today.getMonth() - 3)
  
  toDate.value = today.toISOString().split('T')[0]
  fromDate.value = threeMonthsAgo.toISOString().split('T')[0]
})

const loadMostOrdered = async () => {
  if (!fromDate.value || !toDate.value) return
  
  try {
    loading.value = true
    error.value = null
    
    const supplierId = selectedSupplierId.value || undefined
    const result = await getMostOrderedProducts(fromDate.value, toDate.value, supplierId, 100)
    products.value = result
  } catch (e: any) {
    error.value = e.message || 'Villa við að sækja skýrslu'
    console.error('Error loading most ordered products:', e)
    products.value = []
  } finally {
    loading.value = false
  }
}

const resetFilters = () => {
  const today = new Date()
  const threeMonthsAgo = new Date()
  threeMonthsAgo.setMonth(today.getMonth() - 3)
  
  toDate.value = today.toISOString().split('T')[0]
  fromDate.value = threeMonthsAgo.toISOString().split('T')[0]
  selectedSupplierId.value = ''
  products.value = []
  error.value = null
}

const handleExport = async () => {
  if (!fromDate.value || !toDate.value) return
  
  try {
    loading.value = true
    error.value = null
    
    const supplierId = selectedSupplierId.value || undefined
    await exportMostOrderedToCsv(fromDate.value, toDate.value, supplierId, 500)
  } catch (e: any) {
    error.value = e.message || 'Villa við að sækja CSV'
    console.error('Error exporting CSV:', e)
  } finally {
    loading.value = false
  }
}

const navigateToProduct = (productId: string) => {
  navigateTo(`/products/${productId}`)
}

const formatCurrency = (value: number) => {
  return new Intl.NumberFormat('is-IS', {
    style: 'currency',
    currency: 'ISK',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }).format(value)
}

const formatQuantity = (value: number) => {
  return new Intl.NumberFormat('is-IS', {
    minimumFractionDigits: 0,
    maximumFractionDigits: 3
  }).format(value)
}
</script>

