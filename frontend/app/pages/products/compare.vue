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
          <p class="text-sm text-gray-600">Berðu saman verð mismunandi vara</p>
        </div>
      </div>
      
      <!-- Product Search with Autocomplete -->
      <div class="mt-6">
        <div class="relative">
          <input 
            v-model="searchQuery"
            @input="handleSearchInput"
            @keydown.enter.prevent="handleEnterKey"
            @keydown.down.prevent="selectNext"
            @keydown.up.prevent="selectPrevious"
            @keydown.escape="closeDropdown"
            @blur="handleBlur"
            type="text" 
            placeholder="Leita að vöru eftir nafni..."
            class="w-full px-6 py-4 pr-32 text-lg border-2 border-indigo-200 rounded-xl focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition-all shadow-md"
          />
          <button 
            @click="addSelectedProduct"
            :disabled="!selectedProduct || isLoading"
            class="absolute right-2 top-1/2 -translate-y-1/2 px-6 py-2 bg-gradient-to-r from-indigo-500 to-purple-600 text-white rounded-lg hover:from-indigo-600 hover:to-purple-700 transition-all duration-200 shadow-md hover:shadow-lg font-medium disabled:opacity-50 disabled:cursor-not-allowed"
          >
            Bæta við
          </button>
        </div>

        <!-- Autocomplete Dropdown -->
        <div 
          v-if="showDropdown && searchResults.length > 0"
          class="absolute z-50 w-full mt-2 bg-white rounded-xl shadow-2xl border-2 border-indigo-100 max-h-96 overflow-y-auto"
        >
          <div 
            v-for="(product, index) in searchResults" 
            :key="product.id"
            @click="selectProduct(product)"
            @mouseenter="selectedIndex = index"
            class="px-6 py-4 cursor-pointer transition-colors"
            :class="selectedIndex === index ? 'bg-indigo-50' : 'hover:bg-gray-50'"
          >
            <div class="flex items-center justify-between">
              <div class="flex-1">
                <div class="font-semibold text-gray-800">{{ product.name }}</div>
                <div class="text-sm text-gray-600 mt-1">
                  <span class="font-mono">{{ product.productCode }}</span>
                  <span class="mx-2">•</span>
                  <span>{{ product.supplierName }}</span>
                </div>
              </div>
              <div v-if="product.latestPrice" class="text-right">
                <div class="font-bold text-indigo-600">{{ formatPrice(product.latestPrice) }} kr</div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Selected Products List -->
    <div v-if="selectedProducts.length > 0" class="mb-6">
      <div class="bg-white rounded-2xl shadow-lg border-2 border-gray-100 p-6">
        <div class="flex items-center justify-between mb-4">
          <h2 class="text-lg font-bold text-gray-800">Valdar vörur ({{ selectedProducts.length }})</h2>
          <button 
            @click="clearAll"
            class="px-4 py-2 text-sm text-red-600 hover:text-red-700 hover:bg-red-50 rounded-lg transition-colors font-medium"
          >
            Hreinsa allt
          </button>
        </div>
        <div class="flex flex-wrap gap-3">
          <div 
            v-for="product in selectedProducts" 
            :key="product.id"
            class="flex items-center gap-3 bg-gradient-to-br from-indigo-50 to-purple-50 rounded-xl px-4 py-3 border border-indigo-200"
          >
            <div class="flex-1">
              <div class="font-semibold text-gray-800">{{ product.name }}</div>
              <div class="text-xs text-gray-600 mt-1">
                {{ product.supplierName }} • {{ product.productCode }}
              </div>
            </div>
            <button 
              @click="removeProduct(product.id)"
              class="w-8 h-8 flex items-center justify-center text-red-600 hover:text-red-700 hover:bg-red-100 rounded-lg transition-colors"
            >
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
        </div>
        <div class="mt-4">
          <button 
            @click="performComparison"
            :disabled="isLoading || selectedProducts.length < 2"
            class="w-full px-6 py-3 bg-gradient-to-r from-indigo-500 to-purple-600 text-white rounded-xl hover:from-indigo-600 hover:to-purple-700 transition-all duration-200 shadow-md hover:shadow-lg font-bold text-lg disabled:opacity-50 disabled:cursor-not-allowed"
          >
            <span v-if="!isLoading">Berðu saman vörur</span>
            <span v-else class="flex items-center justify-center gap-2">
              <div class="w-5 h-5 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
              Ber saman...
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
    </div>

    <!-- Comparison Results -->
    <div v-else-if="comparisonResults.length > 0" class="space-y-6">
      <!-- Summary Cards -->
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
              <div v-if="result.listPrice" class="flex justify-between items-center">
                <span class="text-sm text-gray-600">Listaverð:</span>
                <span class="text-sm font-medium text-gray-800">{{ formatPrice(result.listPrice) }} kr</span>
              </div>
              <div v-if="result.discount" class="flex justify-between items-center">
                <span class="text-sm text-gray-600">Afsláttur:</span>
                <span class="text-sm font-medium text-red-600">-{{ formatPrice(result.discount) }} kr</span>
              </div>
              <div class="h-px bg-gray-200"></div>
              <div class="flex justify-between items-center">
                <span class="text-sm font-bold text-gray-700">Lokaverð:</span>
                <span class="text-2xl font-bold text-indigo-600">{{ formatPrice(result.latestPrice || 0) }} kr</span>
              </div>
              <div v-if="result.lastPurchaseDate" class="flex justify-between items-center text-xs text-gray-500">
                <span>Síðasta kaup:</span>
                <span>{{ formatDate(result.lastPurchaseDate) }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Price History Comparison Chart -->
      <div class="bg-white rounded-2xl shadow-xl overflow-hidden border border-gray-100">
        <div class="bg-gradient-to-r from-indigo-500 to-purple-600 px-8 py-6">
          <div class="flex items-center gap-3">
            <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 12l3-3 3 3 4-4M8 21l4-4 4 4M3 4h18M4 4h16v12a1 1 0 01-1 1H5a1 1 0 01-1-1V4z" />
            </svg>
            <h2 class="text-2xl font-bold text-white">Verðþróun</h2>
          </div>
        </div>

        <div class="p-8">
          <ClientOnly>
            <canvas ref="chartCanvas" class="w-full" style="max-height: 500px"></canvas>
            <template #fallback>
              <div class="flex justify-center items-center py-20">
                <div class="text-gray-500">Hleður línuriti...</div>
              </div>
            </template>
          </ClientOnly>
        </div>
      </div>

      <!-- Detailed Comparison Table -->
      <div class="bg-white rounded-2xl shadow-xl overflow-hidden border border-gray-100">
        <div class="bg-gradient-to-r from-indigo-500 to-purple-600 px-8 py-6">
          <h2 class="text-2xl font-bold text-white">Nákvæmur samanburður</h2>
        </div>
        <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-gray-200">
            <thead class="bg-gray-50">
              <tr>
                <th class="px-6 py-4 text-left text-xs font-bold text-gray-700 uppercase tracking-wider">Vara</th>
                <th class="px-6 py-4 text-left text-xs font-bold text-gray-700 uppercase tracking-wider">Birgir</th>
                <th class="px-6 py-4 text-right text-xs font-bold text-gray-700 uppercase tracking-wider">Listaverð</th>
                <th class="px-6 py-4 text-right text-xs font-bold text-gray-700 uppercase tracking-wider">Afsláttur</th>
                <th class="px-6 py-4 text-right text-xs font-bold text-gray-700 uppercase tracking-wider">Lokaverð</th>
                <th class="px-6 py-4 text-right text-xs font-bold text-gray-700 uppercase tracking-wider">Síðasta kaup</th>
              </tr>
            </thead>
            <tbody class="bg-white divide-y divide-gray-200">
              <tr 
                v-for="result in comparisonResults" 
                :key="result.productId"
                class="hover:bg-indigo-50 transition-colors duration-150"
                :class="result.productId === bestPrice?.productId ? 'bg-green-50' : ''"
              >
                <td class="px-6 py-4 whitespace-nowrap">
                  <div class="flex items-center gap-2">
                    <span v-if="result.productId === bestPrice?.productId" class="flex-shrink-0 w-2 h-2 bg-green-500 rounded-full"></span>
                    <div>
                      <div class="text-sm font-medium text-gray-900">{{ result.productName }}</div>
                      <div class="text-xs text-gray-500 font-mono">{{ result.productCode }}</div>
                    </div>
                  </div>
                </td>
                <td class="px-6 py-4 whitespace-nowrap">
                  <span class="text-sm text-gray-900">{{ result.supplierName }}</span>
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-right">
                  <span class="text-sm text-gray-600">{{ formatPrice(result.listPrice || 0) }} kr</span>
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-right">
                  <span class="text-sm font-medium text-red-600">-{{ formatPrice(result.discount || 0) }} kr</span>
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-right">
                  <span class="text-lg font-bold text-indigo-600">{{ formatPrice(result.latestPrice || 0) }} kr</span>
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-right">
                  <span class="text-sm text-gray-600">{{ result.lastPurchaseDate ? formatDate(result.lastPurchaseDate) : 'N/A' }}</span>
                </td>
              </tr>
            </tbody>
          </table>
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
      <h3 class="text-xl font-bold text-gray-700 mb-2">Leitaðu að vörum</h3>
      <p class="text-gray-500">Sláðu inn vörunafn í leitarboxið hér fyrir ofan og bættu við að minnsta kosti 2 vörum til að bera saman</p>
    </div>
  </div>
</template>

<script setup lang="ts">
const { lookupProducts, compareMultipleProducts } = useProducts()

const searchQuery = ref('')
const searchResults = ref<any[]>([])
const showDropdown = ref(false)
const selectedIndex = ref(-1)
const selectedProducts = ref<any[]>([])
const comparisonResults = ref<any[]>([])
const isLoading = ref(false)
const error = ref('')
const chartCanvas = ref<HTMLCanvasElement | null>(null)
let chartInstance: any = null
let searchTimeout: NodeJS.Timeout | null = null

const selectedProduct = computed(() => {
  if (selectedIndex.value >= 0 && selectedIndex.value < searchResults.value.length) {
    return searchResults.value[selectedIndex.value]
  }
  return null
})

const bestPrice = computed(() => {
  if (comparisonResults.value.length === 0) return null
  const withPrice = comparisonResults.value.filter((r: any) => r.latestPrice != null)
  if (withPrice.length === 0) return null
  return withPrice.reduce((best: any, current: any) => 
    current.latestPrice < best.latestPrice ? current : best
  )
})

const handleSearchInput = async () => {
  if (searchTimeout) {
    clearTimeout(searchTimeout)
  }
  
  if (searchQuery.value.length < 3) {
    searchResults.value = []
    showDropdown.value = false
    return
  }

  searchTimeout = setTimeout(async () => {
    try {
      const results = await lookupProducts(searchQuery.value, 10)
      // Filter out already selected products
      searchResults.value = results.filter(p => 
        !selectedProducts.value.some(sp => sp.id === p.id)
      )
      showDropdown.value = searchResults.value.length > 0
      selectedIndex.value = -1
    } catch (err) {
      console.error('Search error:', err)
      searchResults.value = []
      showDropdown.value = false
    }
  }, 300)
}

const handleEnterKey = () => {
  if (selectedProduct.value) {
    addSelectedProduct()
  }
}

const selectNext = () => {
  if (searchResults.value.length > 0) {
    selectedIndex.value = (selectedIndex.value + 1) % searchResults.value.length
  }
}

const selectPrevious = () => {
  if (searchResults.value.length > 0) {
    selectedIndex.value = selectedIndex.value <= 0 
      ? searchResults.value.length - 1 
      : selectedIndex.value - 1
  }
}

const closeDropdown = () => {
  showDropdown.value = false
  selectedIndex.value = -1
}

const handleBlur = () => {
  // Delay to allow click events to fire
  setTimeout(() => {
    closeDropdown()
  }, 200)
}

const selectProduct = (product: any) => {
  if (!selectedProducts.value.some(p => p.id === product.id)) {
    selectedProducts.value.push(product)
    searchQuery.value = ''
    searchResults.value = []
    showDropdown.value = false
    selectedIndex.value = -1
  }
}

const addSelectedProduct = () => {
  if (selectedProduct.value) {
    selectProduct(selectedProduct.value)
  }
}

const removeProduct = (productId: string) => {
  selectedProducts.value = selectedProducts.value.filter(p => p.id !== productId)
}

const clearAll = () => {
  selectedProducts.value = []
  comparisonResults.value = []
  if (chartInstance) {
    chartInstance.destroy()
    chartInstance = null
  }
}

const performComparison = async () => {
  if (selectedProducts.value.length < 2) return

  isLoading.value = true
  error.value = ''
  comparisonResults.value = []

  try {
    const productIds = selectedProducts.value.map(p => p.id)
    const results = await compareMultipleProducts(productIds)
    comparisonResults.value = results

    // Render chart after data is loaded
    await nextTick()
    if (comparisonResults.value.length > 0) {
      await renderChart()
    }
  } catch (err: any) {
    error.value = 'Villa kom upp við samanburð'
    console.error(err)
  } finally {
    isLoading.value = false
  }
}

const renderChart = async () => {
  if (!chartCanvas.value || !comparisonResults.value.length || !process.client) {
    return
  }

  // Destroy existing chart
  if (chartInstance) {
    chartInstance.destroy()
    chartInstance = null
  }

  // Dynamic import for client-side only
  const { Chart, registerables } = await import('chart.js')
  Chart.register(...registerables)

  const ctx = chartCanvas.value.getContext('2d')
  if (!ctx) return

  // Collect all unique dates from all products
  const allDates = new Set<string>()
  comparisonResults.value.forEach((result: any) => {
    if (result.history && Array.isArray(result.history)) {
      result.history.forEach((item: any) => {
        const date = new Date(item.invoiceDate).toISOString().split('T')[0]
        allDates.add(date)
      })
    }
  })

  const sortedDates = Array.from(allDates).sort()

  // Generate colors for each product
  const colors = [
    { border: 'rgb(99, 102, 241)', bg: 'rgba(99, 102, 241, 0.1)' },
    { border: 'rgb(236, 72, 153)', bg: 'rgba(236, 72, 153, 0.1)' },
    { border: 'rgb(34, 197, 94)', bg: 'rgba(34, 197, 94, 0.1)' },
    { border: 'rgb(251, 146, 60)', bg: 'rgba(251, 146, 60, 0.1)' },
    { border: 'rgb(139, 92, 246)', bg: 'rgba(139, 92, 246, 0.1)' },
    { border: 'rgb(59, 130, 246)', bg: 'rgba(59, 130, 246, 0.1)' },
    { border: 'rgb(245, 101, 101)', bg: 'rgba(245, 101, 101, 0.1)' },
    { border: 'rgb(168, 85, 247)', bg: 'rgba(168, 85, 247, 0.1)' },
    { border: 'rgb(20, 184, 166)', bg: 'rgba(20, 184, 166, 0.1)' },
    { border: 'rgb(249, 115, 22)', bg: 'rgba(249, 115, 22, 0.1)' },
  ]

  const datasets = comparisonResults.value.map((result: any, index: number) => {
    const color = colors[index % colors.length]
    const historyMap = new Map()
    
    if (result.history && Array.isArray(result.history)) {
      result.history.forEach((item: any) => {
        const date = new Date(item.invoiceDate).toISOString().split('T')[0]
        historyMap.set(date, item.unitPrice)
      })
    }

    const data = sortedDates.map(date => historyMap.get(date) || null)

    return {
      label: `${result.productName} (${result.supplierName})`,
      data: data,
      borderColor: color.border,
      backgroundColor: color.bg,
      borderWidth: 3,
      tension: 0.4,
      fill: false,
      pointRadius: 4,
      pointHoverRadius: 6,
      spanGaps: false,
    }
  })

  chartInstance = new Chart(ctx, {
    type: 'line',
    data: {
      labels: sortedDates.map(date => 
        new Date(date).toLocaleDateString('is-IS', { year: 'numeric', month: 'short', day: 'numeric' })
      ),
      datasets: datasets
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: {
          display: true,
          position: 'top',
        },
        tooltip: {
          mode: 'index',
          intersect: false,
          callbacks: {
            label: (context: any) => {
              if (context.parsed.y === null) return null
              return `${context.dataset.label}: ${formatPrice(context.parsed.y)} kr`
            }
          }
        }
      },
      scales: {
        y: {
          beginAtZero: false,
          ticks: {
            callback: (value: any) => {
              if (typeof value === 'number') {
                return `${formatPrice(value)} kr`
              }
              return value
            }
          }
        }
      }
    }
  })
}

// Watch for chart canvas and comparison results
watch([chartCanvas, () => comparisonResults.value], async ([canvas, results]) => {
  if (canvas && results && results.length > 0 && process.client) {
    await renderChart()
  }
}, { immediate: true })

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

onUnmounted(() => {
  if (chartInstance) {
    chartInstance.destroy()
  }
  if (searchTimeout) {
    clearTimeout(searchTimeout)
  }
})
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
