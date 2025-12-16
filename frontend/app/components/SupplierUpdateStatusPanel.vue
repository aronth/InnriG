<template>
  <div class="bg-white rounded-lg shadow-md p-6">
    <div class="flex items-center justify-between mb-4">
      <h2 class="text-xl font-bold text-gray-800">
        Tíðni uppfærslna birgja
      </h2>
      <div class="text-sm text-gray-500">
        Markmið: 4x á ári (1x á hvert ársfjórðung)
      </div>
    </div>

    <div v-if="loading" class="text-center py-8">
      <div class="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600"></div>
    </div>

    <div v-else-if="error" class="text-red-600 text-center py-4">
      {{ error }}
    </div>

    <div v-else-if="suppliers.length === 0" class="text-gray-500 text-center py-4">
      Engir birgjar fundust
    </div>

    <div v-else class="space-y-3">
      <div
        v-for="supplier in suppliers"
        :key="supplier.supplierId"
        :class="[
          'border rounded-lg p-4 transition-colors',
          getStatusClass(supplier.status)
        ]"
      >
        <div class="flex items-center justify-between">
          <div class="flex-1">
            <h3 class="font-semibold text-gray-900">{{ supplier.supplierName }}</h3>
            <div class="mt-1 text-sm text-gray-600">
              <span v-if="supplier.lastInvoiceDate">
                Síðasti reikningur: 
                <span class="font-medium">
                  {{ formatDate(supplier.lastInvoiceDate) }}
                </span>
                <span v-if="supplier.lastInvoiceNumber" class="text-gray-500 ml-2">
                  ({{ supplier.lastInvoiceNumber }})
                </span>
              </span>
              <span v-else class="text-gray-400">
                Engir reikningar
              </span>
            </div>
            <div v-if="supplier.daysSinceLastInvoice !== null" class="mt-1 text-xs">
              <span :class="supplier.isOverdue ? 'text-red-600 font-semibold' : 'text-gray-500'">
                {{ formatDaysAgo(supplier.daysSinceLastInvoice) }}
              </span>
            </div>
          </div>
          <div class="ml-4">
            <span
              :class="[
                'px-3 py-1 rounded-full text-xs font-medium',
                getStatusBadgeClass(supplier.status)
              ]"
            >
              {{ getStatusText(supplier.status) }}
            </span>
          </div>
        </div>
      </div>
    </div>

    <div v-if="overdueCount > 0" class="mt-4 p-3 bg-red-50 border border-red-200 rounded-lg">
      <div class="flex items-center">
        <svg class="w-5 h-5 text-red-600 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
        </svg>
        <span class="text-sm font-medium text-red-800">
          {{ overdueCount }} birg{{ overdueCount === 1 ? 'i' : 'jar' }} með yfir 3 mánaða gamla uppfærslu
        </span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { SupplierUpdateStatus } from '~/app/composables/useSupplierStatus'

const { getSupplierUpdateStatus } = useSupplierStatus()

const suppliers = ref<SupplierUpdateStatus[]>([])
const loading = ref(true)
const error = ref<string | null>(null)

const overdueCount = computed(() => 
  suppliers.value.filter(s => s.status === 'Overdue').length
)

const getStatusClass = (status: string) => {
  switch (status) {
    case 'Overdue':
      return 'border-red-300 bg-red-50'
    case 'NoInvoices':
      return 'border-yellow-300 bg-yellow-50'
    default:
      return 'border-green-200 bg-green-50'
  }
}

const getStatusBadgeClass = (status: string) => {
  switch (status) {
    case 'Overdue':
      return 'bg-red-100 text-red-800'
    case 'NoInvoices':
      return 'bg-yellow-100 text-yellow-800'
    default:
      return 'bg-green-100 text-green-800'
  }
}

const getStatusText = (status: string) => {
  switch (status) {
    case 'Overdue':
      return 'Yfir 3 mánuðum'
    case 'NoInvoices':
      return 'Engir reikningar'
    default:
      return 'Í lagi'
  }
}

const formatDate = (dateString: string) => {
  const date = new Date(dateString)
  return new Intl.DateTimeFormat('is-IS', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  }).format(date)
}

const formatDaysAgo = (days: number) => {
  if (days === 0) return 'Í dag'
  if (days === 1) return 'Fyrir 1 degi'
  if (days < 30) return `Fyrir ${days} dögum`
  const months = Math.floor(days / 30)
  if (months === 1) return 'Fyrir 1 mánuði'
  if (months < 12) return `Fyrir ${months} mánuðum`
  const years = Math.floor(months / 12)
  if (years === 1) return 'Fyrir 1 ári'
  return `Fyrir ${years} árum`
}

onMounted(async () => {
  try {
    loading.value = true
    error.value = null
    suppliers.value = await getSupplierUpdateStatus()
  } catch (e: any) {
    error.value = e.message || 'Villa við að sækja upplýsingar'
    console.error('Error fetching supplier status:', e)
  } finally {
    loading.value = false
  }
})
</script>

