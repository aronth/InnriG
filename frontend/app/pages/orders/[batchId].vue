<template>
  <div class="min-h-[80vh] space-y-6">
    <!-- Header -->
    <div class="bg-gradient-to-br from-indigo-50 via-purple-50 to-pink-50 rounded-2xl p-8 border border-indigo-100 shadow-lg">
      <div class="flex items-start justify-between gap-6">
        <div>
          <div class="flex items-center gap-3">
            <NuxtLink
              to="/orders"
              class="inline-flex items-center px-3 py-2 bg-white border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors text-sm font-medium text-gray-700"
            >
              <svg class="w-5 h-5 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
              </svg>
              Til baka
            </NuxtLink>
            <h1 class="text-2xl font-bold text-gray-800">Raðir</h1>
          </div>
          <p class="mt-2 text-sm text-gray-600">
            Batch: <span class="font-mono">{{ batchId }}</span>
          </p>
        </div>

        <div class="flex items-center gap-3">
          <button
            @click="refresh"
            :disabled="isLoading"
            class="inline-flex items-center px-4 py-2 bg-white border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors text-sm font-medium text-gray-700 disabled:opacity-50"
          >
            <svg class="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v6h6M20 20v-6h-6M5 11a7 7 0 0112.9-3M19 13a7 7 0 01-12.9 3" />
            </svg>
            Endurhlaða
          </button>
        </div>
      </div>
    </div>

    <!-- Controls -->
    <div class="bg-white rounded-2xl shadow-lg border border-gray-100 p-6">
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div>
          <label class="block text-xs font-semibold text-gray-600 mb-1">Leita</label>
          <input
            v-model="search"
            type="text"
            placeholder="Pöntunarnúmer, skuldunautur, lýsing..."
            class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
          />
        </div>
        <div>
          <label class="block text-xs font-semibold text-gray-600 mb-1">Síða</label>
          <div class="flex items-center gap-2">
            <button
              @click="prevPage"
              :disabled="isLoading || page === 0"
              class="px-3 py-2 border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50"
            >
              Fyrri
            </button>
            <div class="text-sm text-gray-700">
              {{ page + 1 }}
            </div>
            <button
              @click="nextPage"
              :disabled="isLoading || rows.length < take"
              class="px-3 py-2 border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50"
            >
              Næsta
            </button>
          </div>
        </div>
        <div>
          <label class="block text-xs font-semibold text-gray-600 mb-1">Fjöldi á síðu</label>
          <select
            v-model.number="take"
            @change="onTakeChange"
            class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 bg-white"
          >
            <option :value="50">50</option>
            <option :value="100">100</option>
            <option :value="200">200</option>
            <option :value="500">500</option>
          </select>
        </div>
      </div>
    </div>

    <!-- Content -->
    <div class="bg-white rounded-2xl shadow-lg border border-gray-100 overflow-hidden">
      <div class="p-6 border-b border-gray-100 flex items-center justify-between">
        <h2 class="text-lg font-bold text-gray-800">Pöntunarraðir</h2>
        <p class="text-sm text-gray-500">{{ filteredRows.length }} raðir á skjá (af {{ rows.length }})</p>
      </div>

      <div v-if="isLoading" class="p-10 flex justify-center">
        <div class="relative">
          <div class="w-16 h-16 border-4 border-indigo-200 rounded-full"></div>
          <div class="w-16 h-16 border-4 border-indigo-600 border-t-transparent rounded-full animate-spin absolute top-0"></div>
        </div>
      </div>

      <div v-else-if="error" class="p-6">
        <div class="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-800">
          {{ error }}
        </div>
      </div>

      <div v-else class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200">
          <thead class="bg-gradient-to-r from-indigo-50 to-purple-50">
            <tr>
              <th class="px-6 py-4 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">Pöntunarnr.</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">Skuldunautur</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">Afgreiðsla</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">Uppruni</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">Staða</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">Stofndagur</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">Skannað</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">Skráð út</th>
              <th class="px-6 py-4 text-right text-xs font-medium text-gray-700 uppercase tracking-wider">Biðtími (mín)</th>
              <th class="px-6 py-4 text-right text-xs font-medium text-gray-700 uppercase tracking-wider">Samtals m/VSK</th>
              <th class="px-6 py-4 text-left text-xs font-medium text-gray-700 uppercase tracking-wider">Lýsing</th>
            </tr>
          </thead>
          <tbody class="bg-white divide-y divide-gray-200">
            <tr v-for="r in filteredRows" :key="r.id" class="hover:bg-indigo-50 transition-colors duration-150">
              <td class="px-6 py-4 whitespace-nowrap">
                <span class="text-sm font-medium text-gray-900 font-mono">{{ r.orderNumber || '-' }}</span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap">
                <span class="text-sm text-gray-900">{{ r.debtor || '-' }}</span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap">
                <span class="text-sm text-gray-700">{{ r.deliveryMethod || 'Unknown' }}</span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap">
                <span class="text-sm text-gray-700">{{ r.orderSource || 'Unknown' }}</span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap">
                <span class="text-sm text-gray-700">{{ r.status || '-' }}</span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap">
                <span class="text-sm text-gray-700">{{ formatDateTime(r.createdDate) }}</span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap">
                <span class="text-sm text-gray-700">{{ formatDateTime(r.scannedAt) }}</span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap">
                <span class="text-sm text-gray-700">{{ formatDateTime(r.checkedOutAt) }}</span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-right">
                <span class="text-sm font-bold text-gray-900">{{ r.waitTimeMin ?? '-' }}</span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-right">
                <span class="text-sm font-bold text-gray-900">{{ formatIsk(r.totalAmountWithVat) }}</span>
              </td>
              <td class="px-6 py-4 max-w-[32rem]">
                <div class="text-sm text-gray-700 truncate" :title="r.description || ''">
                  {{ r.description || '-' }}
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
const route = useRoute()
const batchId = computed(() => String(route.params.batchId || ''))

const { getRows } = useOrders()

type OrderRow = {
  id: string
  orderImportBatchId: string
  sourceRowNumber: number
  status?: string | null
  debtor?: string | null
  description?: string | null
  createdDate?: string | null
  scannedAt?: string | null
  checkedOutAt?: string | null
  totalAmountWithVat?: number | null
  orderNumber?: string | null
  deliveryMethod?: string | null
  orderSource?: string | null
  waitTimeMin?: number | null
  createdAt: string
}

const isLoading = ref(false)
const error = ref<string | null>(null)
const rows = ref<OrderRow[]>([])

const page = ref(0)
const take = ref(200)
const search = ref('')

const formatDateTime = (value?: string | null) => {
  if (!value) return '-'
  try {
    return new Date(value).toLocaleString('is-IS')
  } catch {
    return value
  }
}

const formatIsk = (value?: number | null) => {
  if (value == null) return '-'
  try {
    return new Intl.NumberFormat('is-IS', { style: 'currency', currency: 'ISK', maximumFractionDigits: 0 }).format(value)
  } catch {
    return `${value} kr`
  }
}

const refresh = async () => {
  isLoading.value = true
  error.value = null
  try {
    rows.value = await getRows({
      batchId: batchId.value,
      skip: page.value * take.value,
      take: take.value
    })
  } catch (e: any) {
    error.value = e?.data?.message || e?.message || 'Villa kom upp við að sækja raðir.'
  } finally {
    isLoading.value = false
  }
}

const filteredRows = computed(() => {
  const q = search.value.trim().toLowerCase()
  if (!q) return rows.value

  return rows.value.filter((r: OrderRow) => {
    const hay = [
      r.orderNumber,
      r.debtor,
      r.description,
      r.status
    ].filter(Boolean).join(' ').toLowerCase()
    return hay.includes(q)
  })
})

const nextPage = async () => {
  page.value++
  await refresh()
}

const prevPage = async () => {
  page.value = Math.max(0, page.value - 1)
  await refresh()
}

const onTakeChange = async () => {
  page.value = 0
  await refresh()
}

watch(batchId, async () => {
  page.value = 0
  await refresh()
})

onMounted(async () => {
  await refresh()
})
</script>


