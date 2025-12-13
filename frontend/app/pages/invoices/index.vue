<template>
  <div class="min-h-[80vh]">
    <!-- Header -->
    <div class="bg-gradient-to-br from-indigo-50 via-purple-50 to-pink-50 rounded-2xl p-8 border border-indigo-100 shadow-lg mb-6">
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-3">
          <div class="w-12 h-12 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-xl flex items-center justify-center shadow-lg">
            <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
            </svg>
          </div>
          <div>
            <h1 class="text-2xl font-bold text-gray-800">Reikningar</h1>
            <p class="text-sm text-gray-600">{{ invoices.length }} reikningar í kerfinu</p>
          </div>
        </div>
        
        <!-- Filters -->
        <div class="flex items-center gap-4">
          <!-- Supplier Filter -->
          <select 
            v-model="filters.supplierId" 
            @change="fetchInvoices"
            class="px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 bg-white"
          >
            <option value="">Allir birgjar</option>
            <option v-for="supplier in suppliers" :key="supplier.id" :value="supplier.id">
              {{ supplier.name }}
            </option>
          </select>

          <!-- Buyer Filter -->
          <select 
            v-model="filters.buyerId" 
            @change="onBuyerFilterChange"
            class="px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 bg-white"
          >
            <option value="">Allir kaupendur</option>
            <option v-for="buyer in buyers" :key="buyer.id" :value="buyer.id">
              {{ buyer.name || 'Nafnlaus' }} ({{ buyer.taxId }})
            </option>
          </select>

          <!-- Sort By -->
          <select 
            v-model="filters.sortBy" 
            @change="fetchInvoices"
            class="px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 bg-white"
          >
            <option value="invoiceDate">Dagsetning</option>
            <option value="invoiceNumber">Reikningsnúmer</option>
            <option value="totalAmount">Heildarupphæð</option>
            <option value="supplierName">Birgir</option>
            <option value="buyerName">Kaupandi</option>
            <option value="createdAt">Búið til</option>
          </select>

          <!-- Sort Order -->
          <button
            @click="toggleSortOrder"
            class="px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50 focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 bg-white"
            :title="filters.sortOrder === 'asc' ? 'Hækkandi' : 'Lækkandi'"
          >
            <svg 
              v-if="filters.sortOrder === 'asc'"
              class="w-5 h-5 text-gray-600" 
              fill="none" 
              stroke="currentColor" 
              viewBox="0 0 24 24"
            >
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 15l7-7 7 7" />
            </svg>
            <svg 
              v-else
              class="w-5 h-5 text-gray-600" 
              fill="none" 
              stroke="currentColor" 
              viewBox="0 0 24 24"
            >
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
            </svg>
          </button>
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

    <!-- Invoices Table -->
    <div v-else-if="invoices.length > 0" class="bg-white rounded-2xl shadow-lg border border-gray-100 overflow-hidden">
      <div class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200">
          <thead class="bg-gradient-to-r from-indigo-50 to-purple-50">
            <tr>
              <th class="px-6 py-4 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">
                Reikningsnúmer
              </th>
              <th class="px-6 py-4 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">
                Birgir
              </th>
              <th class="px-6 py-4 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">
                Kaupandi
              </th>
              <th class="px-6 py-4 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">
                Dagsetning
              </th>
              <th class="px-6 py-4 text-right text-xs font-medium text-gray-700 uppercase tracking-wider">
                Heildarupphæð
              </th>
              <th class="px-6 py-4 text-center text-xs font-medium text-gray-700 uppercase tracking-wider">
                Fjöldi vara
              </th>
              <th class="px-6 py-4 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">
                Búið til
              </th>
              <th class="px-6 py-4 text-center text-xs font-medium text-gray-700 uppercase tracking-wider">
                Aðgerðir
              </th>
            </tr>
          </thead>
          <tbody class="bg-white divide-y divide-gray-200">
            <tr 
              v-for="invoice in invoices" 
              :key="invoice.id"
              class="hover:bg-indigo-50 transition-colors duration-150"
            >
              <td class="px-6 py-4 whitespace-nowrap">
                <span class="text-sm font-medium text-gray-900 font-mono">{{ invoice.invoiceNumber }}</span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap">
                <span class="text-sm text-gray-900">{{ invoice.supplierName }}</span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap">
                <span class="text-sm text-gray-600">{{ invoice.buyerName || '-' }}</span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap">
                <span class="text-sm text-gray-900">{{ formatDate(invoice.invoiceDate) }}</span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-right">
                <span class="text-sm font-bold text-gray-900">{{ formatPrice(invoice.totalAmount) }} kr</span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-center">
                <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-indigo-100 text-indigo-800">
                  {{ invoice.itemCount }}
                </span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap">
                <span class="text-sm text-gray-500">{{ formatDate(invoice.createdAt) }}</span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-center">
                <div class="flex items-center justify-center gap-2">
                  <NuxtLink
                    :to="`/invoices/${invoice.id}`"
                    class="inline-flex items-center px-3 py-1.5 text-sm font-medium text-indigo-600 hover:text-indigo-800 hover:bg-indigo-50 rounded-lg transition-colors duration-200"
                  >
                    Skoða
                  </NuxtLink>
                  <button
                    @click="confirmDeleteInvoice(invoice)"
                    class="inline-flex items-center px-3 py-1.5 text-sm font-medium text-red-600 hover:text-red-800 hover:bg-red-50 rounded-lg transition-colors duration-200"
                    title="Eyða reikningi"
                  >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                    </svg>
                  </button>
                </div>
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
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
        </svg>
      </div>
      <h3 class="text-xl font-bold text-gray-700 mb-2">Engir reikningar fundust</h3>
      <p class="text-gray-500">Reikningar verða sjálfkrafa búnir til þegar þú lest inn reikninga.</p>
      <NuxtLink 
        to="/"
        class="inline-block mt-6 px-6 py-3 bg-gradient-to-r from-indigo-500 to-purple-600 text-white rounded-lg hover:from-indigo-600 hover:to-purple-700 transition-all duration-200 shadow-md hover:shadow-lg font-medium"
      >
        Lesa inn reikning
      </NuxtLink>
    </div>

    <!-- Delete Confirmation Modal -->
    <div v-if="showDeleteModal && deletingInvoice" class="fixed z-50 inset-0 overflow-y-auto" @click.self="showDeleteModal = false">
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
                <h3 class="text-lg leading-6 font-medium text-gray-900">Eyða reikningi</h3>
                <div class="mt-2">
                  <p class="text-sm text-gray-500">
                    Ertu viss um að þú viljir eyða reikningi <strong>{{ deletingInvoice.invoiceNumber }}</strong> frá {{ deletingInvoice.supplierName }}? 
                    Þessi aðgerð eyðir einnig öllum vörum sem tengjast þessum reikningi og er óafturkræf.
                  </p>
                </div>
              </div>
            </div>
          </div>
          <div class="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse">
            <button
              @click="handleDeleteInvoice"
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
const { getAllInvoices, deleteInvoice } = useInvoices()
const { getAllSuppliers } = useSuppliers()
const { getAllBuyers } = useBuyers()

const invoices = ref<any[]>([])
const suppliers = ref<any[]>([])
const buyers = ref<any[]>([])
const isLoading = ref(true)
const error = ref('')

// Delete modal
const showDeleteModal = ref(false)
const isDeleting = ref(false)
const deletingInvoice = ref<any>(null)

const filters = ref({
  supplierId: '',
  buyerId: '',
  startDate: '',
  endDate: '',
  sortBy: 'invoiceDate',
  sortOrder: 'desc' as 'asc' | 'desc'
})

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

const toggleSortOrder = () => {
  filters.value.sortOrder = filters.value.sortOrder === 'asc' ? 'desc' : 'asc'
  fetchInvoices()
}

const fetchInvoices = async () => {
  isLoading.value = true
  error.value = ''
  
  try {
    const filterParams: any = {
      sortBy: filters.value.sortBy,
      sortOrder: filters.value.sortOrder
    }
    
    if (filters.value.supplierId) {
      filterParams.supplierId = filters.value.supplierId
    }
    if (filters.value.buyerId) {
      filterParams.buyerId = filters.value.buyerId
    }
    if (filters.value.startDate) {
      filterParams.startDate = filters.value.startDate
    }
    if (filters.value.endDate) {
      filterParams.endDate = filters.value.endDate
    }
    
    invoices.value = await getAllInvoices(filterParams)
  } catch (err: any) {
    error.value = err.message || 'Ekki tókst að sækja reikninga'
    console.error(err)
  } finally {
    isLoading.value = false
  }
}

const fetchSuppliers = async () => {
  try {
    suppliers.value = await getAllSuppliers()
  } catch (err: any) {
    console.error('Failed to fetch suppliers:', err)
  }
}

const fetchBuyers = async () => {
  try {
    buyers.value = await getAllBuyers()
    
    // Set default buyer filter from localStorage or use first buyer if only one exists
    if (buyers.value.length > 0) {
      const savedBuyerId = localStorage.getItem('defaultBuyerId')
      if (savedBuyerId && buyers.value.some(b => b.id === savedBuyerId)) {
        filters.value.buyerId = savedBuyerId
      } else if (buyers.value.length === 1) {
        // Auto-select if only one buyer
        filters.value.buyerId = buyers.value[0].id
        localStorage.setItem('defaultBuyerId', buyers.value[0].id)
      }
    }
  } catch (err: any) {
    console.error('Failed to fetch buyers:', err)
  }
}

const onBuyerFilterChange = () => {
  // Save selected buyer as default
  if (filters.value.buyerId) {
    localStorage.setItem('defaultBuyerId', filters.value.buyerId)
  } else {
    localStorage.removeItem('defaultBuyerId')
  }
  fetchInvoices()
}

const confirmDeleteInvoice = (invoice: any) => {
  deletingInvoice.value = invoice
  showDeleteModal.value = true
}

const handleDeleteInvoice = async () => {
  if (!deletingInvoice.value) return
  
  isDeleting.value = true
  try {
    await deleteInvoice(deletingInvoice.value.id)
    showDeleteModal.value = false
    deletingInvoice.value = null
    await fetchInvoices()
  } catch (err: any) {
    alert(err.data?.message || err.message || 'Mistókst að eyða reikningi')
  } finally {
    isDeleting.value = false
  }
}

onMounted(async () => {
  await Promise.all([fetchSuppliers(), fetchBuyers()])
  // Fetch invoices after filters are set
  await fetchInvoices()
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

