<template>
  <div class="min-h-[80vh]">
    <!-- Header -->
    <div class="bg-gradient-to-br from-indigo-50 via-purple-50 to-pink-50 rounded-2xl p-8 border border-indigo-100 shadow-lg mb-6">
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-3">
          <NuxtLink
            to="/invoices"
            class="p-2 hover:bg-white/50 rounded-lg transition-colors duration-200"
          >
            <svg class="w-6 h-6 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
            </svg>
          </NuxtLink>
          <div>
            <h1 class="text-2xl font-bold text-gray-800">Reikningur</h1>
            <p v-if="invoice" class="text-sm text-gray-600">
              {{ invoice.invoiceNumber }} - {{ invoice.supplierName }}
            </p>
          </div>
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
      <NuxtLink
        to="/invoices"
        class="inline-block mt-4 px-6 py-3 bg-gradient-to-r from-indigo-500 to-purple-600 text-white rounded-lg hover:from-indigo-600 hover:to-purple-700 transition-all duration-200 shadow-md hover:shadow-lg font-medium"
      >
        Til baka
      </NuxtLink>
    </div>

    <!-- Invoice Details -->
    <div v-else-if="invoice" class="bg-white rounded-2xl shadow-lg border border-gray-100 overflow-hidden">
      <!-- Invoice Header Info -->
      <div class="bg-gradient-to-r from-indigo-50 to-purple-50 p-6 border-b border-gray-200">
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          <div>
            <p class="text-xs text-gray-500 uppercase tracking-wide mb-1">Birgir</p>
            <p class="text-lg font-semibold text-gray-900">{{ invoice.supplierName }}</p>
          </div>
          <div>
            <p class="text-xs text-gray-500 uppercase tracking-wide mb-1">Kaupandi</p>
            <p class="text-lg font-semibold text-gray-900">{{ invoice.buyerName || '-' }}</p>
          </div>
          <div>
            <p class="text-xs text-gray-500 uppercase tracking-wide mb-1">Reikningsnúmer</p>
            <p class="text-lg font-semibold text-gray-900 font-mono">{{ invoice.invoiceNumber }}</p>
          </div>
          <div>
            <p class="text-xs text-gray-500 uppercase tracking-wide mb-1">Dagsetning</p>
            <p class="text-lg font-semibold text-gray-900">{{ formatDate(invoice.invoiceDate) }}</p>
          </div>
        </div>
      </div>

      <!-- Invoice Items Table -->
      <div class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200">
          <thead class="bg-gray-50">
            <tr>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Vara
              </th>
              <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                Magn
              </th>
              <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                Listaverð
              </th>
              <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                Afsláttur
              </th>
              <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                Nettó Verð
              </th>
              <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                VSK Kóði
              </th>
              <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                Samtals (Nettó)
              </th>
              <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                Samtals (Brúttó)
              </th>
            </tr>
          </thead>
          <tbody class="bg-white divide-y divide-gray-200">
            <tr 
              v-for="item in invoice.items" 
              :key="item.id"
              class="hover:bg-indigo-50 transition-colors duration-150"
            >
              <td class="px-6 py-4 whitespace-nowrap">
                <div>
                  <div class="text-sm font-medium text-gray-900">{{ item.itemName }}</div>
                  <div class="text-sm text-gray-500 font-mono">{{ item.itemId }}</div>
                </div>
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-right">
                <span class="text-sm text-gray-900">{{ formatNumber(item.quantity) }}</span>
                <span class="text-xs text-gray-500 ml-1">{{ item.unit }}</span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-right">
                <span class="text-sm text-gray-600">{{ formatPrice(item.listPrice) }} kr</span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-right">
                <span class="text-sm font-medium text-red-600">-{{ formatPrice(item.discount) }} kr</span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-right">
                <span class="text-sm font-bold text-indigo-600">{{ formatPrice(item.unitPrice) }} kr</span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-right">
                <span class="text-sm text-gray-600 font-mono">{{ item.vatCode }}</span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-right">
                <span class="text-sm font-medium text-gray-900">{{ formatPrice(item.totalPrice) }} kr</span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-right">
                <span class="text-sm font-bold text-gray-900">{{ formatPrice(item.totalPriceWithVat) }} kr</span>
              </td>
            </tr>
          </tbody>
          <tfoot class="bg-gray-50">
            <tr>
              <td colspan="6" class="px-6 py-4 text-right font-bold text-gray-900">
                Heildarupphæð
              </td>
              <td class="px-6 py-4 text-right font-bold text-gray-900">
                {{ formatPrice(invoice.totalAmount) }} kr
              </td>
              <td class="px-6 py-4"></td>
            </tr>
          </tfoot>
        </table>
      </div>

      <!-- Footer Info -->
      <div class="bg-gray-50 p-6 border-t border-gray-200">
        <div class="flex justify-between items-center text-sm text-gray-600">
          <div>
            <span class="font-medium">Búið til:</span>
            <span class="ml-2">{{ formatDate(invoice.createdAt) }}</span>
          </div>
          <div>
            <span class="font-medium">Fjöldi vara:</span>
            <span class="ml-2">{{ invoice.items?.length || 0 }}</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
const route = useRoute()
const { getInvoice } = useInvoices()

const invoice = ref<any>(null)
const isLoading = ref(true)
const error = ref('')

const formatDate = (dateString: string) => {
  if (!dateString) return ''
  const date = new Date(dateString)
  return new Intl.DateTimeFormat('is-IS', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
  }).format(date)
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
    maximumFractionDigits: 3
  }).format(num)
}

const fetchInvoice = async () => {
  isLoading.value = true
  error.value = ''
  
  try {
    const invoiceId = route.params.id as string
    invoice.value = await getInvoice(invoiceId)
  } catch (err: any) {
    if (err.status === 404 || err.statusCode === 404) {
      error.value = 'Reikningur fannst ekki'
    } else {
      error.value = err.message || 'Ekki tókst að sækja reikning'
    }
    console.error(err)
  } finally {
    isLoading.value = false
  }
}

onMounted(() => {
  fetchInvoice()
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

