<template>
  <div class="min-h-[80vh]">
    <!-- Header -->
    <div class="bg-gradient-to-br from-indigo-50 via-purple-50 to-pink-50 rounded-2xl p-8 border border-indigo-100 shadow-lg mb-6">
      <div class="flex items-center justify-between mb-6">
        <div class="flex items-center gap-3">
          <div class="w-12 h-12 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-xl flex items-center justify-center shadow-lg">
            <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
            </svg>
          </div>
          <div>
            <h1 class="text-2xl font-bold text-gray-800">Vörulisti</h1>
            <p class="text-sm text-gray-600">
              <span v-if="paginatedData">{{ paginatedData.totalCount }}</span>
              <span v-else>0</span>
              vörur í kerfinu
              <span v-if="paginatedData && paginatedData.totalCount !== paginatedData.items.length">
                (sýni {{ paginatedData.items.length }} af {{ paginatedData.totalCount }})
              </span>
            </p>
          </div>
        </div>
      </div>

      <!-- Search and Filters -->
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <!-- Search with Autocomplete -->
        <div class="relative">
          <label class="block text-xs font-medium text-gray-700 mb-1">Leita</label>
          <div class="relative">
            <input
              v-model="filters.search"
              @input="handleSearchInput"
              @focus="showSuggestions = true"
              @blur="handleSearchBlur"
              type="text"
              placeholder="Leita að vöru..."
              class="w-full px-4 py-2 pl-10 border border-indigo-200 rounded-lg bg-white shadow-sm focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition-all"
            />
            <svg class="w-5 h-5 text-gray-400 absolute left-3 top-1/2 transform -translate-y-1/2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
            </svg>
            <!-- Autocomplete Suggestions -->
            <div
              v-if="showSuggestions && searchSuggestions.length > 0"
              class="absolute z-50 w-full mt-1 bg-white border border-gray-200 rounded-lg shadow-lg max-h-60 overflow-y-auto"
            >
              <div
                v-for="suggestion in searchSuggestions"
                :key="suggestion.id"
                @mousedown="selectSuggestion(suggestion)"
                class="px-4 py-2 hover:bg-indigo-50 cursor-pointer border-b border-gray-100 last:border-b-0"
              >
                <div class="font-medium text-sm text-gray-900">{{ suggestion.name }}</div>
                <div class="text-xs text-gray-500">{{ suggestion.productCode }} • {{ suggestion.supplierName }}</div>
              </div>
            </div>
          </div>
        </div>

        <!-- Supplier Filter -->
        <div>
          <label class="block text-xs font-medium text-gray-700 mb-1">Birgir</label>
          <select
            v-model="filters.supplierId"
            class="w-full px-4 py-2 border border-indigo-200 rounded-lg bg-white shadow-sm focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition-all"
          >
            <option value="">Allir birgjar</option>
            <option v-for="supplier in suppliers" :key="supplier.id" :value="supplier.id">
              {{ supplier.name }}
            </option>
          </select>
        </div>

        <!-- Price Range -->
        <div class="grid grid-cols-2 gap-2">
          <div>
            <label class="block text-xs font-medium text-gray-700 mb-1">Lágmarksverð</label>
            <input
              v-model.number="filters.minPrice"
              type="number"
              step="0.01"
              min="0"
              placeholder="Min"
              class="w-full px-3 py-2 border border-indigo-200 rounded-lg bg-white shadow-sm focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition-all text-sm"
            />
          </div>
          <div>
            <label class="block text-xs font-medium text-gray-700 mb-1">Hámarksverð</label>
            <input
              v-model.number="filters.maxPrice"
              type="number"
              step="0.01"
              min="0"
              placeholder="Max"
              class="w-full px-3 py-2 border border-indigo-200 rounded-lg bg-white shadow-sm focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition-all text-sm"
            />
          </div>
        </div>

        <!-- Has Price Filter -->
        <div>
          <label class="block text-xs font-medium text-gray-700 mb-1">Verð</label>
          <select
            v-model="filters.hasPrice"
            class="w-full px-4 py-2 border border-indigo-200 rounded-lg bg-white shadow-sm focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition-all"
          >
            <option :value="undefined">Allt</option>
            <option :value="true">Með verði</option>
            <option :value="false">Án verðs</option>
          </select>
        </div>
      </div>

      <!-- Sorting and Page Size -->
      <div class="flex items-center justify-between mt-4 pt-4 border-t border-indigo-200">
        <div class="flex items-center gap-3">
          <label class="text-xs font-medium text-gray-700">Raða eftir:</label>
          <select
            v-model="filters.sortBy"
            class="px-3 py-1.5 border border-indigo-200 rounded-lg bg-white shadow-sm focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition-all text-sm"
          >
            <option value="supplierName">Birgir</option>
            <option value="name">Heiti</option>
            <option value="productCode">Númer</option>
            <option value="latestPrice">Verð</option>
            <option value="lastPurchaseDate">Síðasta kaup</option>
          </select>
          <select
            v-model="filters.sortOrder"
            class="px-3 py-1.5 border border-indigo-200 rounded-lg bg-white shadow-sm focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition-all text-sm"
          >
            <option value="asc">Hækkandi</option>
            <option value="desc">Lækkandi</option>
          </select>
        </div>
        <div class="flex items-center gap-3">
          <label class="text-xs font-medium text-gray-700">Sýna:</label>
          <select
            v-model.number="filters.pageSize"
            class="px-3 py-1.5 border border-indigo-200 rounded-lg bg-white shadow-sm focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition-all text-sm"
          >
            <option :value="25">25</option>
            <option :value="50">50</option>
            <option :value="100">100</option>
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
    <div v-else-if="paginatedData && paginatedData.items.length > 0" class="bg-white rounded-2xl shadow-xl overflow-hidden border border-gray-100">
      <div class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200">
          <thead class="bg-gradient-to-r from-indigo-50 to-purple-50">
            <tr>
              <th class="px-3 py-2 text-left text-xs font-bold text-gray-700 uppercase tracking-wider cursor-pointer hover:bg-indigo-100 transition-colors" @click="toggleSort('productCode')">
                <div class="flex items-center gap-1">
                  Númer
                  <svg v-if="filters.sortBy === 'productCode'" class="w-3 h-3" :class="filters.sortOrder === 'asc' ? '' : 'rotate-180'" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 15l7-7 7 7" />
                  </svg>
                </div>
              </th>
              <th class="px-3 py-2 text-left text-xs font-bold text-gray-700 uppercase tracking-wider cursor-pointer hover:bg-indigo-100 transition-colors" @click="toggleSort('name')">
                <div class="flex items-center gap-1">
                  Heiti
                  <svg v-if="filters.sortBy === 'name'" class="w-3 h-3" :class="filters.sortOrder === 'asc' ? '' : 'rotate-180'" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 15l7-7 7 7" />
                  </svg>
                </div>
              </th>
              <th class="px-3 py-2 text-left text-xs font-bold text-gray-700 uppercase tracking-wider cursor-pointer hover:bg-indigo-100 transition-colors" @click="toggleSort('supplierName')">
                <div class="flex items-center gap-1">
                  Birgir
                  <svg v-if="filters.sortBy === 'supplierName'" class="w-3 h-3" :class="filters.sortOrder === 'asc' ? '' : 'rotate-180'" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 15l7-7 7 7" />
                  </svg>
                </div>
              </th>
              <th class="px-3 py-2 text-right text-xs font-bold text-gray-700 uppercase tracking-wider">
                Listaverð
              </th>
              <th class="px-3 py-2 text-right text-xs font-bold text-gray-700 uppercase tracking-wider">
                Afsláttur
              </th>
              <th class="px-3 py-2 text-right text-xs font-bold text-gray-700 uppercase tracking-wider cursor-pointer hover:bg-indigo-100 transition-colors" @click="toggleSort('latestPrice')">
                <div class="flex items-center justify-end gap-1">
                  Verð
                  <svg v-if="filters.sortBy === 'latestPrice'" class="w-3 h-3" :class="filters.sortOrder === 'asc' ? '' : 'rotate-180'" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 15l7-7 7 7" />
                  </svg>
                </div>
              </th>
              <th class="px-3 py-2 text-right text-xs font-bold text-gray-700 uppercase tracking-wider">
              </th>
            </tr>
          </thead>
          <tbody class="bg-white divide-y divide-gray-200">
            <tr v-for="product in paginatedData.items" :key="product.id" class="hover:bg-indigo-50 transition-colors duration-150">
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
                  :to="{
                    path: `/products/${product.id}`,
                    query: filters.supplierId ? { supplierId: filters.supplierId } : {}
                  }"
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

      <!-- Pagination Controls -->
      <div v-if="paginatedData && paginatedData.totalPages > 1" class="bg-gray-50 px-6 py-4 border-t border-gray-200 flex items-center justify-between">
        <div class="text-sm text-gray-700">
          Síða {{ paginatedData.page }} af {{ paginatedData.totalPages }}
        </div>
        <div class="flex items-center gap-2">
          <button
            @click="goToPage(paginatedData.page - 1)"
            :disabled="!paginatedData.hasPreviousPage"
            :class="[
              'px-4 py-2 text-sm font-medium rounded-lg transition-all',
              paginatedData.hasPreviousPage
                ? 'bg-white text-indigo-600 border border-indigo-200 hover:bg-indigo-50 shadow-sm'
                : 'bg-gray-100 text-gray-400 cursor-not-allowed'
            ]"
          >
            Fyrri
          </button>
          <div class="flex items-center gap-1">
            <button
              v-for="pageNum in visiblePages"
              :key="pageNum"
              @click="goToPage(pageNum)"
              :class="[
                'px-3 py-2 text-sm font-medium rounded-lg transition-all',
                pageNum === paginatedData.page
                  ? 'bg-indigo-600 text-white shadow-md'
                  : 'bg-white text-indigo-600 border border-indigo-200 hover:bg-indigo-50 shadow-sm'
              ]"
            >
              {{ pageNum }}
            </button>
          </div>
          <button
            @click="goToPage(paginatedData.page + 1)"
            :disabled="!paginatedData.hasNextPage"
            :class="[
              'px-4 py-2 text-sm font-medium rounded-lg transition-all',
              paginatedData.hasNextPage
                ? 'bg-white text-indigo-600 border border-indigo-200 hover:bg-indigo-50 shadow-sm'
                : 'bg-gray-100 text-gray-400 cursor-not-allowed'
            ]"
          >
            Næsta
          </button>
        </div>
      </div>
    </div>

    <!-- Empty State -->
    <div v-else-if="!isLoading && (!paginatedData || paginatedData.items.length === 0)" class="bg-gradient-to-br from-gray-50 to-gray-100 rounded-2xl p-12 text-center border-2 border-dashed border-gray-300">
      <div class="w-20 h-20 bg-gray-200 rounded-full flex items-center justify-center mx-auto mb-4">
        <svg class="w-10 h-10 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
        </svg>
      </div>
      <h3 class="text-xl font-bold text-gray-700 mb-2">
        {{ filters.search || filters.supplierId || filters.minPrice !== undefined || filters.maxPrice !== undefined || filters.hasPrice !== undefined
          ? 'Engar vörur fundust með þessum síum'
          : 'Engar vörur fundust' }}
      </h3>
      <p class="text-gray-500">
        {{ filters.search || filters.supplierId || filters.minPrice !== undefined || filters.maxPrice !== undefined || filters.hasPrice !== undefined
          ? 'Reyndu að breyta síunum eða leit.'
          : 'Byrjaðu að lesa inn reikninga til að sjá vörur hér.' }}
      </p>
      <button
        v-if="filters.search || filters.supplierId || filters.minPrice !== undefined || filters.maxPrice !== undefined || filters.hasPrice !== undefined"
        @click="clearFilters"
        class="inline-block mt-6 px-6 py-3 bg-gradient-to-r from-indigo-500 to-purple-600 text-white rounded-lg hover:from-indigo-600 hover:to-purple-700 transition-all duration-200 shadow-md hover:shadow-lg font-medium"
      >
        Hreinsa síur
      </button>
      <NuxtLink 
        v-else
        to="/products"
        class="inline-block mt-6 px-6 py-3 bg-gradient-to-r from-indigo-500 to-purple-600 text-white rounded-lg hover:from-indigo-600 hover:to-purple-700 transition-all duration-200 shadow-md hover:shadow-lg font-medium"
      >
        Lesa inn reikning
      </NuxtLink>
    </div>
  </div>
</template>

<script setup lang="ts">
const route = useRoute()
const router = useRouter()
const { getAllProducts, lookupProducts } = useProducts()
const { getAllSuppliers } = useSuppliers()

const paginatedData = ref<Awaited<ReturnType<typeof getAllProducts>> | null>(null)
const suppliers = ref<any[]>([])
const searchSuggestions = ref<Awaited<ReturnType<typeof lookupProducts>>>([])
const showSuggestions = ref(false)
const isLoading = ref(true)
const error = ref('')

// Debounce timer for search
let searchDebounceTimer: ReturnType<typeof setTimeout> | null = null
let fetchDebounceTimer: ReturnType<typeof setTimeout> | null = null

// Filters
const filters = ref<{
  supplierId?: string
  search?: string
  minPrice?: number
  maxPrice?: number
  hasPrice?: boolean
  sortBy?: string
  sortOrder?: 'asc' | 'desc'
  page?: number
  pageSize?: number
}>({
  supplierId: '',
  search: '',
  minPrice: undefined,
  maxPrice: undefined,
  hasPrice: undefined,
  sortBy: 'supplierName',
  sortOrder: 'asc',
  page: 1,
  pageSize: 50
})

// Initialize filters from query params
const initializeFromQuery = () => {
  const query = route.query
  if (query.supplierId) filters.value.supplierId = query.supplierId as string
  if (query.search) filters.value.search = query.search as string
  if (query.minPrice) filters.value.minPrice = parseFloat(query.minPrice as string)
  if (query.maxPrice) filters.value.maxPrice = parseFloat(query.maxPrice as string)
  if (query.hasPrice !== undefined) filters.value.hasPrice = query.hasPrice === 'true'
  if (query.sortBy) filters.value.sortBy = query.sortBy as string
  if (query.sortOrder) filters.value.sortOrder = query.sortOrder as 'asc' | 'desc'
  if (query.page) filters.value.page = parseInt(query.page as string)
  if (query.pageSize) filters.value.pageSize = parseInt(query.pageSize as string)
}

// Update URL query params when filters change
const updateQueryParams = () => {
  const query: Record<string, string> = {}
  if (filters.value.supplierId) query.supplierId = filters.value.supplierId
  if (filters.value.search) query.search = filters.value.search
  if (filters.value.minPrice !== undefined) query.minPrice = filters.value.minPrice.toString()
  if (filters.value.maxPrice !== undefined) query.maxPrice = filters.value.maxPrice.toString()
  if (filters.value.hasPrice !== undefined) query.hasPrice = filters.value.hasPrice.toString()
  if (filters.value.sortBy && filters.value.sortBy !== 'supplierName') query.sortBy = filters.value.sortBy
  if (filters.value.sortOrder && filters.value.sortOrder !== 'asc') query.sortOrder = filters.value.sortOrder
  if (filters.value.page && filters.value.page !== 1) query.page = filters.value.page.toString()
  if (filters.value.pageSize && filters.value.pageSize !== 50) query.pageSize = filters.value.pageSize.toString()
  
  router.push({ query })
}

// Fetch products with current filters
const fetchData = async () => {
  isLoading.value = true
  error.value = ''
  
  try {
    // Fetch suppliers first if we don't have them
    if (suppliers.value.length === 0) {
      suppliers.value = await getAllSuppliers()
    }
    
    // Build filters object, removing undefined values
    const filterParams: {
      supplierId?: string
      search?: string
      minPrice?: number
      maxPrice?: number
      hasPrice?: boolean
      sortBy?: string
      sortOrder?: 'asc' | 'desc'
      page?: number
      pageSize?: number
    } = {
      page: filters.value.page || 1,
      pageSize: filters.value.pageSize || 50,
      sortBy: filters.value.sortBy || 'supplierName',
      sortOrder: filters.value.sortOrder || 'asc'
    }
    
    if (filters.value.supplierId) filterParams.supplierId = filters.value.supplierId
    if (filters.value.search) filterParams.search = filters.value.search
    if (filters.value.minPrice !== undefined) filterParams.minPrice = filters.value.minPrice
    if (filters.value.maxPrice !== undefined) filterParams.maxPrice = filters.value.maxPrice
    if (filters.value.hasPrice !== undefined) filterParams.hasPrice = filters.value.hasPrice
    
    paginatedData.value = await getAllProducts(filterParams)
  } catch (err: any) {
    error.value = err.message || 'Ekki tókst að sækja gögn'
    console.error(err)
  } finally {
    isLoading.value = false
  }
}

// Debounced fetch for filter changes
const debouncedFetch = () => {
  if (fetchDebounceTimer) clearTimeout(fetchDebounceTimer)
  fetchDebounceTimer = setTimeout(() => {
    filters.value.page = 1 // Reset to first page on filter change
    updateQueryParams()
    fetchData()
  }, 300)
}

// Handle search input with autocomplete
const handleSearchInput = async () => {
  const query = filters.value.search || ''
  
  // Update URL and fetch data
  debouncedFetch()
  
  // Show autocomplete suggestions if query is 3+ characters
  if (query.length >= 3) {
    if (searchDebounceTimer) clearTimeout(searchDebounceTimer)
    searchDebounceTimer = setTimeout(async () => {
      try {
        searchSuggestions.value = await lookupProducts(query, 10)
      } catch (err) {
        console.error('Failed to fetch search suggestions:', err)
        searchSuggestions.value = []
      }
    }, 300)
  } else {
    searchSuggestions.value = []
  }
}

// Handle search blur (hide suggestions after a delay to allow clicks)
const handleSearchBlur = () => {
  setTimeout(() => {
    showSuggestions.value = false
  }, 200)
}

// Select a suggestion
const selectSuggestion = (suggestion: Awaited<ReturnType<typeof lookupProducts>>[0]) => {
  filters.value.search = suggestion.name
  searchSuggestions.value = []
  showSuggestions.value = false
  filters.value.page = 1
  updateQueryParams()
  fetchData()
}

// Toggle sort order
const toggleSort = (sortBy: string) => {
  if (filters.value.sortBy === sortBy) {
    filters.value.sortOrder = filters.value.sortOrder === 'asc' ? 'desc' : 'asc'
  } else {
    filters.value.sortBy = sortBy
    filters.value.sortOrder = 'asc'
  }
  debouncedFetch()
}

// Pagination
const goToPage = (page: number) => {
  if (page < 1 || (paginatedData.value && page > paginatedData.value.totalPages)) return
  filters.value.page = page
  updateQueryParams()
  fetchData()
}

// Clear all filters
const clearFilters = () => {
  filters.value = {
    supplierId: '',
    search: '',
    minPrice: undefined,
    maxPrice: undefined,
    hasPrice: undefined,
    sortBy: 'supplierName',
    sortOrder: 'asc',
    page: 1,
    pageSize: 50
  }
  searchSuggestions.value = []
  updateQueryParams()
  fetchData()
}

// Calculate visible page numbers for pagination
const visiblePages = computed(() => {
  if (!paginatedData.value) return []
  const current = paginatedData.value.page
  const total = paginatedData.value.totalPages
  const pages: number[] = []
  
  if (total <= 7) {
    // Show all pages if 7 or fewer
    for (let i = 1; i <= total; i++) {
      pages.push(i)
    }
  } else {
    // Show first, last, current, and nearby pages
    if (current <= 3) {
      for (let i = 1; i <= 5; i++) pages.push(i)
      pages.push(total)
    } else if (current >= total - 2) {
      pages.push(1)
      for (let i = total - 4; i <= total; i++) pages.push(i)
    } else {
      pages.push(1)
      for (let i = current - 1; i <= current + 1; i++) pages.push(i)
      pages.push(total)
    }
  }
  
  return pages
})

// Watch filter changes
watch(() => filters.value.supplierId, debouncedFetch)
watch(() => filters.value.minPrice, debouncedFetch)
watch(() => filters.value.maxPrice, debouncedFetch)
watch(() => filters.value.hasPrice, debouncedFetch)
watch(() => filters.value.sortBy, debouncedFetch)
watch(() => filters.value.sortOrder, debouncedFetch)
watch(() => filters.value.pageSize, () => {
  filters.value.page = 1
  debouncedFetch()
})

// Watch route query params to handle browser back/forward
watch(() => route.query, () => {
  initializeFromQuery()
  fetchData()
}, { deep: true })

// Fetch on mount
onMounted(() => {
  initializeFromQuery()
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
