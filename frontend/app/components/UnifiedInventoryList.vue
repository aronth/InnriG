<template>
  <div class="bg-white rounded-lg shadow-md p-6">
    <div class="flex items-center justify-between mb-6">
      <h2 class="text-2xl font-bold text-gray-800">Sameiginlegur vörulisti</h2>
      <button
        @click="handleExport"
        :disabled="loading || items.length === 0"
        class="px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed"
      >
        Sækja CSV
      </button>
    </div>

    <!-- Filters -->
    <div class="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
      <div>
        <label class="block text-sm font-medium text-gray-700 mb-2">
          Birgir
        </label>
        <select
          v-model="selectedSupplierId"
          @change="loadData"
          class="w-full px-3 py-2 border border-gray-300 rounded-md"
        >
          <option value="">Allir birgjar</option>
          <option
            v-for="supplier in suppliers"
            :key="supplier.id"
            :value="supplier.id"
          >
            {{ supplier.name }}
          </option>
        </select>
      </div>
      <div>
        <label class="block text-sm font-medium text-gray-700 mb-2">
          Leita
        </label>
        <input
          v-model="searchQuery"
          @input="debouncedSearch"
          type="text"
          placeholder="Vörunúmer, vöruheiti..."
          class="w-full px-3 py-2 border border-gray-300 rounded-md"
        />
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="text-center py-8">
      <div class="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600"></div>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="text-red-600 text-center py-4">
      {{ error }}
    </div>

    <!-- Empty State -->
    <div v-else-if="items.length === 0" class="text-gray-500 text-center py-8">
      Engar vörur fundust
    </div>

    <!-- Table -->
    <div v-else class="overflow-x-auto">
      <table class="min-w-full divide-y divide-gray-200">
        <thead class="bg-gray-50">
          <tr>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Birgir
            </th>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Vörunúmer
            </th>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Vöruheiti
            </th>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Eining
            </th>
            <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
              Verð án VSK
            </th>
            <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
              Skilagjöld / umbúðagjöld
            </th>
            <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
              Nettó innkaupsverð
            </th>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Síðasta uppfærsla
            </th>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
              Reikningsnúmer
            </th>
          </tr>
        </thead>
        <tbody class="bg-white divide-y divide-gray-200">
          <tr v-for="(item, index) in items" :key="index" class="hover:bg-gray-50">
            <td class="px-4 py-3 whitespace-nowrap text-sm font-medium text-gray-900">
              {{ item.birgir }}
            </td>
            <td class="px-4 py-3 whitespace-nowrap text-sm text-gray-700">
              {{ item.vorunumer }}
            </td>
            <td class="px-4 py-3 text-sm text-gray-700">
              {{ item.voruheiti }}
            </td>
            <td class="px-4 py-3 whitespace-nowrap text-sm text-gray-700">
              {{ item.eining }}
            </td>
            <td class="px-4 py-3 whitespace-nowrap text-sm text-right text-gray-900">
              {{ formatCurrency(item.verdAnVsk) }}
            </td>
            <td class="px-4 py-3 whitespace-nowrap text-sm text-right text-gray-700">
              {{ formatCurrency(item.skilagjoldUmbudagjold) }}
            </td>
            <td class="px-4 py-3 whitespace-nowrap text-sm text-right font-medium text-gray-900">
              {{ formatCurrency(item.nettoInnkaupsverdPerEiningu) }}
            </td>
            <td class="px-4 py-3 whitespace-nowrap text-sm text-gray-700">
              {{ formatDate(item.dagsetningSidustuUppfaerslu) }}
            </td>
            <td class="px-4 py-3 whitespace-nowrap text-sm text-gray-500">
              {{ item.sidastiReikningsnumer || '-' }}
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Summary -->
    <div v-if="items.length > 0" class="mt-4 text-sm text-gray-600">
      Samtals {{ items.length }} vör{{ items.length === 1 ? 'a' : 'ur' }}
    </div>
  </div>
</template>

<script setup lang="ts">
import type { UnifiedInventoryListItem } from '~/app/composables/useInventoryList'

const { getUnifiedList, exportToCsv } = useInventoryList()
const { getAllSuppliers } = useSuppliers()

const items = ref<UnifiedInventoryListItem[]>([])
const suppliers = ref<any[]>([])
const loading = ref(true)
const error = ref<string | null>(null)
const selectedSupplierId = ref('')
const searchQuery = ref('')

let searchTimeout: NodeJS.Timeout | null = null

const debouncedSearch = () => {
  if (searchTimeout) {
    clearTimeout(searchTimeout)
  }
  searchTimeout = setTimeout(() => {
    loadData()
  }, 500)
}

const loadData = async () => {
  try {
    loading.value = true
    error.value = null
    items.value = await getUnifiedList(
      selectedSupplierId.value || undefined,
      searchQuery.value || undefined
    )
  } catch (e: any) {
    error.value = e.message || 'Villa við að sækja vörulista'
    console.error('Error loading inventory list:', e)
  } finally {
    loading.value = false
  }
}

const handleExport = async () => {
  try {
    await exportToCsv(
      selectedSupplierId.value || undefined,
      searchQuery.value || undefined
    )
  } catch (e: any) {
    error.value = e.message || 'Villa við að sækja CSV'
    console.error('Error exporting CSV:', e)
  }
}

const formatCurrency = (value: number) => {
  return new Intl.NumberFormat('is-IS', {
    style: 'currency',
    currency: 'ISK',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }).format(value)
}

const formatDate = (dateString: string) => {
  const date = new Date(dateString)
  return new Intl.DateTimeFormat('is-IS', {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  }).format(date)
}

onMounted(async () => {
  try {
    suppliers.value = await getAllSuppliers()
    await loadData()
  } catch (e: any) {
    error.value = e.message || 'Villa við að sækja birgja'
    console.error('Error loading suppliers:', e)
  }
})
</script>

