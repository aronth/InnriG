<template>
  <div class="min-h-[80vh]">
    <!-- Header -->
    <div class="bg-gradient-to-br from-indigo-50 via-purple-50 to-pink-50 rounded-2xl p-8 border border-indigo-100 shadow-lg mb-6">
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-3">
          <div class="w-12 h-12 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-xl flex items-center justify-center shadow-lg">
            <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
            </svg>
          </div>
          <div>
            <h1 class="text-2xl font-bold text-gray-800">Vörulisti</h1>
            <p class="text-sm text-gray-600">{{ products.length }} vörur í kerfinu</p>
          </div>
        </div>
        
        <!-- Filter by Supplier -->
        <div class="flex gap-3">
          <select 
            v-model="selectedSupplierId" 
            class="px-4 py-2 border border-indigo-200 rounded-lg bg-white shadow-sm focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition-all"
          >
            <option value="">Allir birgjar</option>
            <option v-for="supplier in suppliers" :key="supplier.id" :value="supplier.id">
              {{ supplier.name }}
            </option>
          </select>
        </div>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="flex justify-center items-center py-20">
      <div class="relative">
        <div class="w-20 h-20 border-4 border-indigo-200 rounded-full"></div>
        <div class="w-20 h-20 border-4 border-indigo-600 border-t-transparent rounded-full animate-spin absolute top-0"></div>
      </div>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="bg-red-50 border-2 border-red-200 rounded-2xl p-8 text-center">
      <div class="w-16 h-16 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-4">
        <svg class="w-8 h-8 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
        </svg>
      </div>
      <h3 class="text-xl font-bold text-red-900 mb-2">Villa kom upp</h3>
      <p class="text-red-700">{{ error }}</p>
    </div>

    <!-- Products Table -->
    <div v-else-if="products.length > 0" class="bg-white rounded-2xl shadow-xl overflow-hidden border border-gray-100">
      <div class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200">
          <thead class="bg-gradient-to-r from-indigo-50 to-purple-50">
            <tr>
              <th class="px-3 py-2 text-left text-xs font-bold text-gray-700 uppercase tracking-wider">
                Númer
              </th>
              <th class="px-3 py-2 text-left text-xs font-bold text-gray-700 uppercase tracking-wider">
                Heiti
              </th>
              <th class="px-3 py-2 text-left text-xs font-bold text-gray-700 uppercase tracking-wider">
                Birgir
              </th>
              <th class="px-3 py-2 text-right text-xs font-bold text-gray-700 uppercase tracking-wider">
                Listaverð
              </th>
              <th class="px-3 py-2 text-right text-xs font-bold text-gray-700 uppercase tracking-wider">
                Afsláttur
              </th>
              <th class="px-3 py-2 text-right text-xs font-bold text-gray-700 uppercase tracking-wider">
                Verð
              </th>
              <th class="px-3 py-2 text-right text-xs font-bold text-gray-700 uppercase tracking-wider">
              </th>
            </tr>
          </thead>
          <tbody class="bg-white divide-y divide-gray-200">
            <tr v-for="product in products" :key="product.id" class="hover:bg-indigo-50 transition-colors duration-150">
              <td class="px-3 py-2 whitespace-nowrap">
                <div class="text-xs font-medium text-gray-900">{{ product.productCode }}</div>
                <div class="text-xs text-gray-500">{{ product.currentUnit || '-' }}</div>
              </td>
              <td class="px-3 py-2">
                <div class="text-xs text-gray-900 font-medium">{{ product.name }}</div>
              </td>
              <td class="px-3 py-2 whitespace-nowrap">
                <span class="px-2 py-0.5 inline-flex text-xs font-medium rounded-full bg-indigo-100 text-indigo-800">
                  {{ product.supplierName || 'N/A' }}
                </span>
              </td>
              <td class="px-3 py-2 whitespace-nowrap text-right">
                <div v-if="product.listPrice" class="text-xs text-gray-600">
                  {{ formatPrice(product.listPrice) }}
                </div>
                <div v-else class="text-xs text-gray-400">-</div>
              </td>
              <td class="px-3 py-2 whitespace-nowrap text-right">
                <div v-if="product.discount && product.discountPercentage">
                  <div class="text-xs font-medium text-red-600">{{ formatPrice(product.discount) }}</div>
                  <div class="text-xs text-red-500">({{ formatPercent(product.discountPercentage) }}%)</div>
                </div>
                <div v-else class="text-xs text-gray-400">-</div>
              </td>
              <td class="px-3 py-2 whitespace-nowrap text-right">
                <div v-if="product.latestPrice" class="text-sm font-bold text-indigo-600">
                  {{ formatPrice(product.latestPrice) }}
                </div>
                <div v-else class="text-xs text-gray-400">-</div>
              </td>
              <td class="px-3 py-2 whitespace-nowrap text-right">
                <NuxtLink 
                  :to="`/products/${product.id}`"
                  class="inline-flex items-center gap-1 px-3 py-1.5 text-xs bg-gradient-to-r from-indigo-500 to-purple-600 text-white rounded-lg hover:from-indigo-600 hover:to-purple-700 transition-all duration-200 shadow-sm hover:shadow-md font-medium"
                >
                  <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
                  </svg>
                  Skoða
                </NuxtLink>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Empty State -->
    <div v-else class="bg-gradient-to-br from-gray-50 to-gray-100 rounded-2xl p-12 text-center border-2 border-dashed border-gray-300">
      <div class="w-20 h-20 bg-gray-200 rounded-full flex items-center justify-center mx-auto mb-4">
        <svg class="w-10 h-10 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
        </svg>
      </div>
      <h3 class="text-xl font-bold text-gray-700 mb-2">Engar vörur fundust</h3>
      <p class="text-gray-500">Byrjaðu að lesa inn reikninga til að sjá vörur hér.</p>
      <NuxtLink 
        to="/products"
        class="inline-block mt-6 px-6 py-3 bg-gradient-to-r from-indigo-500 to-purple-600 text-white rounded-lg hover:from-indigo-600 hover:to-purple-700 transition-all duration-200 shadow-md hover:shadow-lg font-medium"
      >
        Lesa inn reikning
      </NuxtLink>
    </div>
  </div>
</template>

<script setup lang="ts">
const { getAllProducts } = useProducts()
const { getAllSuppliers } = useSuppliers()

const products = ref<any[]>([])
const suppliers = ref<any[]>([])
const selectedSupplierId = ref('')
const isLoading = ref(true)
const error = ref('')

const fetchData = async () => {
  isLoading.value = true
  error.value = ''
  
  try {
    // Fetch suppliers first if we don't have them
    if (suppliers.value.length === 0) {
      suppliers.value = await getAllSuppliers()
    }
    
    // Fetch products with optional supplier filter
    const supplierId = selectedSupplierId.value || undefined
    products.value = await getAllProducts(supplierId)
  } catch (err: any) {
    error.value = err.message || 'Ekki tókst að sækja gögn'
    console.error(err)
  } finally {
    isLoading.value = false
  }
}

// Watch supplier filter
watch(selectedSupplierId, () => {
  fetchData()
})

// Fetch on mount
onMounted(() => {
  fetchData()
})

const formatPrice = (price: number) => {
  return new Intl.NumberFormat('is-IS', { 
    minimumFractionDigits: 2,
    maximumFractionDigits: 2 
  }).format(price)
}

const formatPercent = (percent: number) => {
  return new Intl.NumberFormat('is-IS', { 
    minimumFractionDigits: 1,
    maximumFractionDigits: 1 
  }).format(percent)
}
</script>

<style scoped>
@keyframes spin {
  from {
    transform: rotate(0deg);
  }
  to {
    transform: rotate(360deg);
  }
}
</style>
