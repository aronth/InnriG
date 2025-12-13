<template>
  <div class="min-h-[80vh]">
    <!-- Header -->
    <div class="bg-gradient-to-br from-indigo-50 via-purple-50 to-pink-50 rounded-2xl p-8 border border-indigo-100 shadow-lg mb-6">
      <div class="flex items-center gap-3 mb-4">
        <div class="w-12 h-12 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-xl flex items-center justify-center shadow-lg">
          <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 7h6m0 10v-3m-3 3h.01M9 17h.01M9 14h.01M12 14h.01M15 11h.01M12 11h.01M9 11h.01M7 21h10a2 2 0 002-2V5a2 2 0 00-2-2H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
          </svg>
        </div>
        <div>
          <h1 class="text-2xl font-bold text-gray-800">Verðsamanburður</h1>
          <p class="text-sm text-gray-600">Berðu saman verð sömu vöru frá mismunandi birgjum</p>
        </div>
      </div>
      
      <!-- Search Bar -->
      <div class="mt-6">
        <div class="relative">
          <input 
            v-model="searchCode"
            @keyup.enter="searchProduct"
            type="text" 
            placeholder="Sláðu inn vörunúmer..."
            class="w-full px-6 py-4 pr-32 text-lg border-2 border-indigo-200 rounded-xl focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition-all shadow-md"
          />
          <button 
            @click="searchProduct"
            :disabled="!searchCode || isLoading"
            class="absolute right-2 top-1/2 -translate-y-1/2 px-6 py-2 bg-gradient-to-r from-indigo-500 to-purple-600 text-white rounded-lg hover:from-indigo-600 hover:to-purple-700 transition-all duration-200 shadow-md hover:shadow-lg font-medium disabled:opacity-50 disabled:cursor-not-allowed"
          >
            <span v-if="!isLoading">Leita</span>
            <span v-else class="flex items-center gap-2">
              <div class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
              Leita...
            </span>
          </button>
        </div>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading && !comparisonResults.length" class="flex justify-center items-center py-20">
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
      <h3 class="text-xl font-bold text-red-900 mb-2">{{ error }}</h3>
      <p class="text-red-700">Engin vara fannst með þessu vörunúmeri</p>
    </div>

    <!-- Comparison Results -->
    <div v-else-if="comparisonResults.length > 0">
      <!-- Best Price Badge -->
      <div class="bg-gradient-to-r from-green-50 to-emerald-50 border-2 border-green-200 rounded-2xl p-6 mb-6">
        <div class="flex items-center gap-3">
          <div class="w-12 h-12 bg-gradient-to-br from-green-500 to-emerald-600 rounded-xl flex items-center justify-center shadow-lg">
            <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </div>
          <div>
            <h3 class="text-lg font-bold text-gray-800">Besta verðið</h3>
            <p class="text-2xl font-bold text-green-700">{{ formatPrice(bestPrice?.latestPrice) }} kr</p>
            <p class="text-sm text-gray-600">frá {{ bestPrice?.supplierName }}</p>
          </div>
        </div>
      </div>

      <!-- Comparison Cards -->
      <div class="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
        <div 
          v-for="result in comparisonResults" 
          :key="result.productId"
          class="bg-white rounded-2xl shadow-lg border-2 transition-all duration-300 hover:shadow-xl"
          :class="result.productId === bestPrice?.productId ? 'border-green-400 bg-green-50' : 'border-gray-100 hover:border-indigo-300'"
        >
          <!-- Best Price Badge -->
          <div v-if="result.productId === bestPrice?.productId" class="bg-gradient-to-r from-green-500 to-emerald-600 text-white px-4 py-2 rounded-t-xl">
            <div class="flex items-center gap-2 justify-center">
              <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
              </svg>
              <span class="font-bold text-sm">BESTA VERÐIÐ</span>
            </div>
          </div>

          <div class="p-6">
            <!-- Supplier Name -->
            <div class="flex items-center gap-2 mb-4">
              <div class="w-10 h-10 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-lg flex items-center justify-center">
                <span class="text-white font-bold text-sm">{{ result.supplierName.substring(0, 2).toUpperCase() }}</span>
              </div>
              <div>
                <h3 class="text-lg font-bold text-gray-800">{{ result.supplierName }}</h3>
                <p class="text-xs text-gray-500">{{ result.productCode }}</p>
              </div>
            </div>

            <!-- Product Name -->
            <p class="text-sm text-gray-700 mb-4 font-medium">{{ result.productName }}</p>

            <!-- Price Info -->
            <div class="space-y-2">
              <div class="flex justify-between items-center">
                <span class="text-sm text-gray-600">Listaverð:</span>
                <span class="text-sm font-medium text-gray-800">{{ formatPrice(result.listPrice) }} kr</span>
              </div>
              <div class="flex justify-between items-center">
                <span class="text-sm text-gray-600">Afsláttur:</span>
                <span class="text-sm font-medium text-red-600">-{{ formatPrice(result.discount) }} kr</span>
              </div>
              <div class="h-px bg-gray-200"></div>
              <div class="flex justify-between items-center">
                <span class="text-sm font-bold text-gray-700">Lokaverð:</span>
                <span class="text-2xl font-bold text-indigo-600">{{ formatPrice(result.latestPrice) }} kr</span>
              </div>
              <div class="flex justify-between items-center text-xs text-gray-500">
                <span>Síðasta kaup:</span>
                <span>{{ formatDate(result.lastPurchaseDate) }}</span>
              </div>
            </div>

            <!-- View History Button -->
            <NuxtLink 
              :to="`/products/catalog/${result.productId}`"
              class="mt-4 w-full inline-flex items-center justify-center gap-2 px-4 py-2 bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200 transition-all duration-200 font-medium"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
              </svg>
              Skoða sögu
            </NuxtLink>
          </div>
        </div>
      </div>
    </div>

    <!-- Empty State -->
    <div v-else class="bg-gradient-to-br from-gray-50 to-gray-100 rounded-2xl p-12 text-center border-2 border-dashed border-gray-300">
      <div class="w-20 h-20 bg-indigo-100 rounded-full flex items-center justify-center mx-auto mb-4">
        <svg class="w-10 h-10 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
        </svg>
      </div>
      <h3 class="text-xl font-bold text-gray-700 mb-2">Leitaðu að vöru</h3>
      <p class="text-gray-500">Sláðu inn vörunúmer í leitarboxið hér fyrir ofan til að bera saman verð</p>
    </div>
  </div>
</template>

<script setup lang="ts">
const { compareProductPrices } = useProducts()

const searchCode = ref('')
const comparisonResults = ref<any[]>([])
const isLoading = ref(false)
const error = ref('')

const bestPrice = computed(() => {
  if (comparisonResults.value.length === 0) return null
  return comparisonResults.value[0] // Results are already sorted by price
})

const searchProduct = async () => {
  if (!searchCode.value.trim()) return
  
  isLoading.value = true
  error.value = ''
  comparisonResults.value = []
  
  try {
    const results = await compareProductPrices(searchCode.value.trim())
    comparisonResults.value = results
    
    if (results.length === 0) {
      error.value = 'Villa kom upp'
    }
  } catch (err: any) {
    error.value = 'Villa kom upp'
    console.error(err)
  } finally {
    isLoading.value = false
  }
}

const formatPrice = (price: number) => {
  return new Intl.NumberFormat('is-IS', { 
    minimumFractionDigits: 2,
    maximumFractionDigits: 2 
  }).format(price)
}

const formatDate = (date: string) => {
  return new Date(date).toLocaleDateString('is-IS', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  })
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
