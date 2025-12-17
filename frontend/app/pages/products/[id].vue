<template>
  <div class="min-h-[80vh]">
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
      <NuxtLink 
        :to="{
          path: '/products/catalog',
          query: route.query.supplierId ? { supplierId: route.query.supplierId } : {}
        }"
        class="inline-block mt-6 px-6 py-3 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors font-medium"
      >
        Til baka í vörulista
      </NuxtLink>
    </div>

    <!-- Product Detail -->
    <div v-else-if="productData">
      <!-- Back Button -->
      <NuxtLink 
        :to="{
          path: '/products/catalog',
          query: route.query.supplierId ? { supplierId: route.query.supplierId } : {}
        }"
        class="inline-flex items-center gap-2 text-indigo-600 hover:text-indigo-800 mb-6 font-medium transition-colors"
      >
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
        </svg>
        Til baka í vörulista
      </NuxtLink>

      <!-- Product Header Card -->
      <div class="bg-gradient-to-br from-indigo-50 via-purple-50 to-pink-50 rounded-2xl p-8 border border-indigo-100 shadow-lg mb-6">
        <div class="flex items-start justify-between">
          <div class="flex items-start gap-4">
            <div class="w-16 h-16 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-xl flex items-center justify-center shadow-lg flex-shrink-0">
              <span class="text-white font-bold text-xl">{{ productData.product.productCode.substring(0, 2) }}</span>
            </div>
            <div>
              <h1 class="text-3xl font-bold text-gray-800 mb-2">{{ productData.product.name }}</h1>
              <div class="flex flex-wrap gap-3 items-center">
                <span class="px-3 py-1 bg-indigo-100 text-indigo-800 rounded-full text-sm font-semibold">
                  {{ productData.product.productCode }}
                </span>
                <span class="px-3 py-1 bg-purple-100 text-purple-800 rounded-full text-sm font-semibold">
                  {{ productData.product.supplier?.name || 'N/A' }}
                </span>
                <span v-if="productData.product.currentUnit" class="px-3 py-1 bg-pink-100 text-pink-800 rounded-full text-sm font-semibold">
                  {{ productData.product.currentUnit }}
                </span>
              </div>
              <p v-if="productData.product.description" class="text-gray-600 mt-3">{{ productData.product.description }}</p>
            </div>
          </div>
        </div>
      </div>

      <!-- Price History Chart -->
      <div class="bg-white rounded-2xl shadow-xl overflow-hidden border border-gray-100 mb-6">
        <div class="bg-gradient-to-r from-indigo-500 to-purple-600 px-8 py-6">
          <div class="flex items-center gap-3">
            <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 12l3-3 3 3 4-4M8 21l4-4 4 4M3 4h18M4 4h16v12a1 1 0 01-1 1H5a1 1 0 01-1-1V4z" />
            </svg>
            <h2 class="text-2xl font-bold text-white">Verðþróun</h2>
            <span class="ml-auto px-3 py-1 bg-white/20 backdrop-blur-sm rounded-full text-sm font-semibold text-white">
              {{ productData.history.length }} færslur
            </span>
          </div>
        </div>

        <div class="p-8">
          <ClientOnly>
            <canvas ref="chartCanvas" class="w-full" style="max-height: 400px"></canvas>
            <template #fallback>
              <div class="flex justify-center items-center py-20">
                <div class="text-gray-500">Hleður línuriti...</div>
              </div>
            </template>
          </ClientOnly>
        </div>
      </div>

      <!-- Price History Table -->
      <div class="bg-white rounded-2xl shadow-xl overflow-hidden border border-gray-100">
        <div class="bg-gradient-to-r from-indigo-500 to-purple-600 px-8 py-6">
          <div class="flex items-center gap-3">
            <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
            </svg>
            <h2 class="text-2xl font-bold text-white">Verðsaga</h2>
          </div>
        </div>

        <!-- History Table -->
        <div v-if="productData.history.length > 0" class="overflow-x-auto">
          <table class="min-w-full divide-y divide-gray-200">
            <thead class="bg-gray-50">
              <tr>
                <th class="px-6 py-4 text-left text-xs font-bold text-gray-700 uppercase tracking-wider">
                  Dagsetning
                </th>
                <th class="px-6 py-4 text-left text-xs font-bold text-gray-700 uppercase tracking-wider">
                  Reikningsnr.
                </th>
                <th class="px-6 py-4 text-left text-xs font-bold text-gray-700 uppercase tracking-wider">
                  Kaupandi
                </th>
                <th class="px-6 py-4 text-right text-xs font-bold text-gray-700 uppercase tracking-wider">
                  Magn
                </th>
                <th class="px-6 py-4 text-right text-xs font-bold text-gray-700 uppercase tracking-wider">
                  Listaverð
                </th>
                <th class="px-6 py-4 text-right text-xs font-bold text-gray-700 uppercase tracking-wider">
                  Afsláttur
                </th>
                <th class="px-6 py-4 text-right text-xs font-bold text-gray-700 uppercase tracking-wider">
                  Einingarverð
                </th>
                <th class="px-6 py-4 text-right text-xs font-bold text-gray-700 uppercase tracking-wider">
                  Heildarverð
                </th>
              </tr>
            </thead>
            <tbody class="bg-white divide-y divide-gray-200">
              <tr 
                v-for="(item, index) in productData.history" 
                :key="item.invoiceId"
                class="hover:bg-indigo-50 transition-colors duration-150"
                :class="index === 0 ? 'bg-green-50' : ''"
              >
                <td class="px-6 py-4 whitespace-nowrap">
                  <div class="flex items-center gap-2">
                    <span v-if="index === 0" class="flex-shrink-0 w-2 h-2 bg-green-500 rounded-full"></span>
                    <span class="text-sm font-medium text-gray-900">{{ formatDate(item.invoiceDate) }}</span>
                  </div>
                </td>
                <td class="px-6 py-4 whitespace-nowrap">
                  <span class="text-sm text-gray-600 font-mono">{{ item.invoiceNumber }}</span>
                </td>
                <td class="px-6 py-4 whitespace-nowrap">
                  <span v-if="item.buyerName" class="text-sm text-gray-700">{{ item.buyerName }}</span>
                  <span v-else class="text-sm text-gray-400 italic">Ekki tilgreint</span>
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-right">
                  <span class="text-sm font-semibold text-gray-900">{{ formatNumber(item.quantity) }}</span>
                  <span class="text-xs text-gray-500 ml-1">{{ item.unit }}</span>
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-right">
                  <span class="text-sm text-gray-600">{{ formatPrice(item.listPrice) }} kr</span>
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-right">
                  <span v-if="item.discount != null && item.discount > 0" class="text-sm font-medium text-red-600">-{{ formatPercent(item.discount) }}%</span>
                  <span v-else class="text-sm text-gray-400">-</span>
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-right">
                  <span class="text-sm font-bold text-indigo-600">{{ formatPrice(item.unitPrice) }} kr</span>
                </td>
                <td class="px-6 py-4 whitespace-nowrap text-right">
                  <span class="text-sm font-bold text-gray-900">{{ formatPrice(item.totalPrice) }} kr</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
const route = useRoute()
const { getProductHistory } = useProducts()

const productData = ref<any>(null)
const isLoading = ref(true)
const error = ref('')
const chartCanvas = ref<HTMLCanvasElement | null>(null)
let chartInstance: any = null

const productId = computed(() => route.params.id as string)

const fetchProductHistory = async () => {
  isLoading.value = true
  error.value = ''
  
  try {
    productData.value = await getProductHistory(productId.value)
  } catch (err: any) {
    error.value = err.message || 'Ekki tókst að sækja verðsögu'
    console.error(err)
  } finally {
    isLoading.value = false
  }
}

// Watch for when both canvas and data are available
watch([chartCanvas, () => productData.value], async ([canvas, data]) => {
  if (canvas && data && process.client) {
    console.log('Canvas and data both available, rendering chart...')
    await renderChart()
  }
}, { immediate: true })

const renderChart = async () => {
  if (!chartCanvas.value || !productData.value?.history) {
    console.log('Canvas or history not available', {
      hasCanvas: !!chartCanvas.value,
      hasData: !!productData.value,
      hasHistory: !!productData.value?.history
    })
    return
  }
  
  // Destroy existing chart if any
  if (chartInstance) {
    chartInstance.destroy()
  }
  
  // Dynamic import for client-side only
  const { Chart, registerables } = await import('chart.js')
  Chart.register(...registerables)
  
  const history = productData.value.history
  const ctx = chartCanvas.value.getContext('2d')
  if (!ctx) {
    console.log('No canvas context')
    return
  }
  
  // Reverse for chronological order (oldest first)
  const reversedHistory = [...history].reverse()
  
  console.log('Creating chart with', reversedHistory.length, 'data points')
  
  chartInstance = new Chart(ctx, {
    type: 'line',
    data: {
      labels: reversedHistory.map((item: any) => 
        new Date(item.invoiceDate).toLocaleDateString('is-IS', { year: 'numeric', month: 'short', day: 'numeric' })
      ),
      datasets: [
        {
          label: 'Einingarverð',
          data: reversedHistory.map((item: any) => item.unitPrice),
          borderColor: 'rgb(99, 102, 241)',
          backgroundColor: 'rgba(99, 102, 241, 0.1)',
          borderWidth: 3,
          tension: 0.4,
          fill: true,
          pointRadius: 5,
          pointHoverRadius: 7,
        },
        {
          label: 'Listaverð',
          data: reversedHistory.map((item: any) => item.listPrice),
          borderColor: 'rgb(156, 163, 175)',
          backgroundColor: 'rgba(156, 163, 175, 0.05)',
          borderWidth: 2,
          borderDash: [5, 5],
          tension: 0.4,
          fill: false,
          pointRadius: 3,
          pointHoverRadius: 5,
        }
      ]
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
              return `${context.dataset.label}: ${formatPrice(context.parsed.y)} kr`
            }
          }
        }
      },
      scales: {
        y: {
          beginAtZero: true,
          ticks: {
            callback: (value: any) => `${formatPrice(value as number)} kr`
          }
        }
      }
    }
  })
  
  console.log('Chart created successfully')
}

const formatPrice = (price: number) => {
  return new Intl.NumberFormat('is-IS', { 
    minimumFractionDigits: 2,
    maximumFractionDigits: 2 
  }).format(price)
}

const formatNumber = (num: number) => {
  return new Intl.NumberFormat('is-IS', { 
    minimumFractionDigits: 0,
    maximumFractionDigits: 2 
  }).format(num)
}

const formatDate = (date: string) => {
  return new Date(date).toLocaleDateString('is-IS', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  })
}

const formatPercent = (percent: number) => {
  return new Intl.NumberFormat('is-IS', {
    minimumFractionDigits: 1,
    maximumFractionDigits: 1
  }).format(percent)
}

onMounted(() => {
  fetchProductHistory()
})

onUnmounted(() => {
  if (chartInstance) {
    chartInstance.destroy()
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
