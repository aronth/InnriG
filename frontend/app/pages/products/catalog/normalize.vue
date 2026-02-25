<template>
  <div class="min-h-[80vh]">
    <!-- Loading list -->
    <div v-if="loadingList" class="flex justify-center items-center py-20">
      <div class="relative">
        <div class="w-20 h-20 border-4 border-indigo-200 rounded-full"></div>
        <div class="w-20 h-20 border-4 border-indigo-600 border-t-transparent rounded-full animate-spin absolute top-0"></div>
      </div>
    </div>

    <!-- Error -->
    <div v-else-if="listError" class="bg-red-50 border-2 border-red-200 rounded-2xl p-8 text-center">
      <h3 class="text-xl font-bold text-red-900 mb-2">Villa kom upp</h3>
      <p class="text-red-700">{{ listError }}</p>
      <NuxtLink
        :to="catalogLink"
        class="inline-block mt-6 px-6 py-3 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 font-medium"
      >
        Til baka í vörulista
      </NuxtLink>
    </div>

    <!-- Empty list -->
    <div v-else-if="productItems.length === 0" class="bg-gradient-to-br from-gray-50 to-gray-100 rounded-2xl p-12 text-center border-2 border-dashed border-gray-300">
      <h3 class="text-xl font-bold text-gray-700 mb-2">Engar vörur fundust</h3>
      <p class="text-gray-500 mb-6">Það eru engar vörur sem passa við síurnar (eða allar hafa þegar margfaldara).</p>
      <NuxtLink
        :to="catalogLink"
        class="inline-block px-6 py-3 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 font-medium"
      >
        Til baka í vörulista
      </NuxtLink>
    </div>

    <!-- Product-by-product view -->
    <div v-else>
      <!-- Back link -->
      <NuxtLink
        :to="catalogLink"
        class="inline-flex items-center gap-2 text-indigo-600 hover:text-indigo-800 mb-6 font-medium"
      >
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
        </svg>
        Til baka í vörulista
      </NuxtLink>

      <!-- Progress -->
      <div class="mb-6 flex items-center justify-between">
        <span class="text-sm font-medium text-gray-700">Vara {{ currentIndex + 1 }} af {{ productItems.length }}</span>
        <div class="w-48 h-2 bg-gray-200 rounded-full overflow-hidden">
          <div
            class="h-full bg-indigo-600 rounded-full transition-all duration-300"
            :style="{ width: progressPercent + '%' }"
          />
        </div>
      </div>

      <!-- End of list -->
      <div v-if="currentIndex >= productItems.length" class="bg-green-50 border border-green-200 rounded-2xl p-8 text-center">
        <h3 class="text-xl font-bold text-green-900 mb-2">Allir liðir</h3>
        <p class="text-green-700 mb-6">Þú ert búinn að fara í gegnum allar vörurnar.</p>
        <NuxtLink
          :to="catalogLink"
          class="inline-block px-6 py-3 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 font-medium"
        >
          Til baka í vörulista
        </NuxtLink>
      </div>

      <!-- Loading current product -->
      <div v-else-if="loadingProduct" class="flex justify-center py-12">
        <div class="w-12 h-12 border-4 border-indigo-200 border-t-indigo-600 rounded-full animate-spin"></div>
      </div>

      <!-- Current product card + form -->
      <div v-else-if="currentProduct" class="space-y-6">
        <!-- Product header -->
        <div class="bg-gradient-to-br from-indigo-50 via-purple-50 to-pink-50 rounded-2xl p-8 border border-indigo-100 shadow-lg">
          <div class="flex items-start gap-4">
            <div class="w-16 h-16 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-xl flex items-center justify-center shadow-lg flex-shrink-0">
              <span class="text-white font-bold text-xl">{{ (currentProduct.productCode || '').substring(0, 2) }}</span>
            </div>
            <div>
              <h1 class="text-2xl font-bold text-gray-800 mb-2">{{ currentProduct.name }}</h1>
              <div class="flex flex-wrap gap-3 items-center">
                <span class="px-3 py-1 bg-indigo-100 text-indigo-800 rounded-full text-sm font-semibold">
                  {{ currentProduct.productCode }}
                </span>
                <span class="px-3 py-1 bg-purple-100 text-purple-800 rounded-full text-sm font-semibold">
                  {{ currentProduct.supplier?.name || currentSupplierName || 'N/A' }}
                </span>
                <span v-if="currentProduct.currentUnit" class="px-3 py-1 bg-pink-100 text-pink-800 rounded-full text-sm font-semibold">
                  {{ currentProduct.currentUnit }}
                </span>
              </div>
              <p v-if="currentProduct.description" class="text-gray-600 mt-3">{{ currentProduct.description }}</p>
              <p v-if="latestPrice != null" class="mt-2 text-lg font-semibold text-indigo-600">
                Núverandi einingarverð: {{ formatPrice(latestPrice) }} kr
              </p>
            </div>
          </div>
        </div>

        <!-- Form -->
        <div class="bg-white rounded-2xl shadow-xl border border-gray-100 p-8">
          <h2 class="text-lg font-bold text-gray-800 mb-4">Stilling eininga</h2>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Margfaldari</label>
              <input
                v-model="formMultiplierStr"
                type="text"
                inputmode="decimal"
                placeholder="t.d. 0,1 fyrir 10 kg"
                class="w-full px-4 py-2 border border-indigo-200 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
              />
              <p class="text-xs text-gray-500 mt-1">Verð × margfaldari = verð á einingu (t.d. á kg)</p>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Eining</label>
              <select
                v-model="formBaseUnit"
                class="w-full px-4 py-2 border border-indigo-200 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-transparent"
              >
                <option value="">—</option>
                <option value="kg">kg</option>
                <option value="L">L</option>
                <option value="m">m</option>
                <option value="stk">stk</option>
              </select>
            </div>
          </div>
          <div v-if="normalizedPreview != null" class="mt-4 p-4 bg-indigo-50 rounded-lg">
            <span class="text-sm font-medium text-indigo-800">Verð á einingu: {{ formatPrice(normalizedPreview) }} kr / {{ formBaseUnit || '—' }}</span>
          </div>

          <!-- Actions -->
          <div class="flex flex-wrap items-center gap-3 mt-8 pt-6 border-t border-gray-200">
            <button
              :disabled="saving"
              class="px-5 py-2.5 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 disabled:opacity-50 font-medium"
              @click="saveAndNext"
            >
              {{ saving ? 'Vista...' : 'Vista og næsta' }}
            </button>
            <button
              class="px-5 py-2.5 bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200 font-medium"
              @click="skipNext"
            >
              Frábært / Næsta
            </button>
            <button
              :disabled="currentIndex === 0"
              class="px-5 py-2.5 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed font-medium"
              @click="goPrevious"
            >
              Fyrri
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { ProductListDto } from '~/composables/useProducts'

const route = useRoute()
const router = useRouter()
const { getAllProducts, getProduct, updateProduct } = useProducts()
const { parseDecimal, formatDecimalForInput } = useDecimalInput()

const productItems = ref<ProductListDto[]>([])
const loadingList = ref(true)
const listError = ref('')
const currentIndex = ref(0)
const currentProduct = ref<Record<string, any> | null>(null)
const loadingProduct = ref(false)
const formMultiplierStr = ref('')
const formBaseUnit = ref('')
const saving = ref(false)

const querySupplierId = computed(() => (route.query.supplierId as string) || '')
const queryBuyerId = computed(() => (route.query.buyerId as string) || '')
const queryHasMultiplier = computed(() => {
  const v = route.query.hasMultiplier
  if (v === 'false') return false
  if (v === 'true') return true
  return undefined
})
const queryPageSize = computed(() => {
  const v = route.query.pageSize
  return v ? parseInt(v as string, 10) : 10000
})
const queryIndex = computed(() => {
  const v = route.query.index
  return v ? Math.max(0, parseInt(v as string, 10)) : 0
})

const catalogLink = computed(() => {
  const query: Record<string, string> = {}
  if (querySupplierId.value) query.supplierId = querySupplierId.value
  if (queryBuyerId.value) query.buyerId = queryBuyerId.value
  return { path: '/products/catalog', query }
})

const progressPercent = computed(() => {
  if (productItems.value.length === 0) return 0
  return Math.round(((currentIndex.value + 1) / productItems.value.length) * 100)
})

const latestPrice = computed(() => {
  if (!currentProduct.value) return null
  const p = currentProduct.value as any
  if (p.latestPrice != null) return p.latestPrice
  const items = p.invoiceItems
  if (Array.isArray(items) && items.length > 0) {
    const sorted = [...items].sort((a: any, b: any) => new Date(b.invoice?.invoiceDate || 0).getTime() - new Date(a.invoice?.invoiceDate || 0).getTime())
    return sorted[0]?.unitPrice
  }
  const cur = productItems.value[currentIndex.value]
  return cur?.latestPrice ?? null
})

const normalizedPreview = computed(() => {
  const price = latestPrice.value
  if (price == null) return null
  const mult = parseDecimal(formMultiplierStr.value)
  if (mult == null) return null
  return price * mult
})

function formatPrice(price: number) {
  return new Intl.NumberFormat('is-IS', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(price)
}

function buildQuery(index: number) {
  const q: Record<string, string> = {}
  if (querySupplierId.value) q.supplierId = querySupplierId.value
  if (queryBuyerId.value) q.buyerId = queryBuyerId.value
  if (queryHasMultiplier.value !== undefined) q.hasMultiplier = queryHasMultiplier.value.toString()
  if (queryPageSize.value !== 10000) q.pageSize = queryPageSize.value.toString()
  if (index > 0) q.index = index.toString()
  return q
}

function syncUrl(index: number) {
  router.replace({ path: route.path, query: buildQuery(index) })
}

async function loadList() {
  loadingList.value = true
  listError.value = ''
  try {
    const filters: any = {
      page: 1,
      pageSize: queryPageSize.value,
      sortBy: 'supplierName',
      sortOrder: 'asc'
    }
    if (querySupplierId.value) filters.supplierId = querySupplierId.value
    if (queryBuyerId.value) filters.buyerId = queryBuyerId.value
    if (queryHasMultiplier.value !== undefined) filters.hasMultiplier = queryHasMultiplier.value
    const result = await getAllProducts(filters)
    productItems.value = result.items || []
    currentIndex.value = Math.min(queryIndex.value, Math.max(0, productItems.value.length - 1))
    syncUrl(currentIndex.value)
  } catch (e: any) {
    listError.value = e?.data?.message || e?.message || 'Ekki tókst að sækja vörulista'
  } finally {
    loadingList.value = false
  }
}

async function loadCurrentProduct() {
  const id = productItems.value[currentIndex.value]?.id
  if (!id) {
    currentProduct.value = null
    return
  }
  loadingProduct.value = true
  currentProduct.value = null
  try {
    const p = await getProduct(id) as any
    currentProduct.value = p
    formMultiplierStr.value = p.normalizedUnitMultiplier != null ? formatDecimalForInput(p.normalizedUnitMultiplier) : ''
    formBaseUnit.value = p.normalizedBaseUnit ?? ''
  } catch {
    currentProduct.value = null
  } finally {
    loadingProduct.value = false
  }
}

const currentSupplierName = computed(() => productItems.value[currentIndex.value]?.supplierName ?? '')

async function saveAndNext() {
  if (!currentProduct.value || saving.value) return
  const id = currentProduct.value.id
  const p = currentProduct.value as Record<string, unknown>
  // Send only scalar fields; nested Supplier/InvoiceItems cause validation errors on PUT
  const mult = parseDecimal(formMultiplierStr.value)
  const payload = {
    id: p.id,
    supplierId: p.supplierId,
    productCode: p.productCode,
    name: p.name,
    description: p.description ?? null,
    currentUnit: p.currentUnit ?? null,
    normalizedUnitMultiplier: mult,
    normalizedBaseUnit: formBaseUnit.value || null,
    createdAt: p.createdAt,
    updatedAt: p.updatedAt
  }
  saving.value = true
  try {
    await updateProduct(id, payload)
    if (currentIndex.value < productItems.value.length - 1) {
      currentIndex.value++
      syncUrl(currentIndex.value)
      await loadCurrentProduct()
    } else {
      currentIndex.value++
      currentProduct.value = null
      syncUrl(currentIndex.value)
    }
  } catch (e: any) {
    alert(e?.data?.message || e?.message || 'Villa við að vista')
  } finally {
    saving.value = false
  }
}

async function skipNext() {
  if (currentIndex.value < productItems.value.length - 1) {
    currentIndex.value++
    syncUrl(currentIndex.value)
    await loadCurrentProduct()
  } else {
    currentIndex.value++
    currentProduct.value = null
    syncUrl(currentIndex.value)
  }
}

async function goPrevious() {
  if (currentIndex.value <= 0) return
  currentIndex.value--
  syncUrl(currentIndex.value)
  await loadCurrentProduct()
}

watch(currentIndex, () => {
  if (productItems.value.length > 0 && currentIndex.value < productItems.value.length) {
    loadCurrentProduct()
  }
}, { immediate: false })

onMounted(async () => {
  await loadList()
  if (productItems.value.length > 0) {
    await loadCurrentProduct()
  }
})

watch(() => route.query, () => {
  const idx = queryIndex.value
  if (idx !== currentIndex.value && idx >= 0 && idx < productItems.value.length) {
    currentIndex.value = idx
    loadCurrentProduct()
  }
}, { deep: true })
</script>
