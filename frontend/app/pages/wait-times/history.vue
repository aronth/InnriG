<template>
  <div class="min-h-[80vh]">
    <WaitTimeSubNav />
    <div class="mb-8">
      <h1 class="text-3xl font-bold text-gray-800 mb-2">Saga biðtíma</h1>
      <p class="text-gray-600">Skoða söguleg gögn um biðtíma</p>
    </div>

    <div class="bg-white rounded-lg shadow p-6 mb-6">
      <div class="grid md:grid-cols-3 gap-4">
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-2">Veitingastaður</label>
          <select
            v-model="filters.restaurant"
            class="w-full border rounded-lg px-3 py-2"
          >
            <option :value="undefined">Allir</option>
            <option :value="0">Greifinn</option>
            <option :value="1">Spretturinn</option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-2">Frá</label>
          <input
            v-model="filters.from"
            type="date"
            class="w-full border rounded-lg px-3 py-2"
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-2">Til</label>
          <input
            v-model="filters.to"
            type="date"
            class="w-full border rounded-lg px-3 py-2"
          />
        </div>
      </div>
      <button
        @click="loadRecords"
        class="mt-4 px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors"
      >
        Sækja gögn
      </button>
    </div>

    <div v-if="loading" class="text-center py-12">
      <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600 mx-auto"></div>
    </div>

    <div v-else-if="error" class="bg-red-50 border border-red-200 rounded-lg p-4 mb-6">
      <p class="text-red-800">{{ error }}</p>
    </div>

    <div v-else-if="records.length === 0" class="bg-gray-50 rounded-lg p-8 text-center text-gray-500">
      <p>Engin gögn fundust</p>
    </div>

    <div v-else class="bg-white rounded-lg shadow overflow-hidden">
      <div class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200">
          <thead class="bg-gray-50">
            <tr>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Veitingastaður
              </th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Sótt (mín)
              </th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Sent/Heimsent (mín)
              </th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Staða
              </th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Tími
              </th>
            </tr>
          </thead>
          <tbody class="bg-white divide-y divide-gray-200">
            <tr v-for="record in records" :key="record.id">
              <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                {{ record.restaurantName }}
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                {{ record.sottMinutes ?? '-' }}
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                {{ record.sentMinutes ?? '-' }}
              </td>
              <td class="px-6 py-4 whitespace-nowrap">
                <span
                  :class="[
                    'px-2 py-1 text-xs rounded-full',
                    record.isClosed
                      ? 'bg-red-100 text-red-800'
                      : 'bg-green-100 text-green-800'
                  ]"
                >
                  {{ record.isClosed ? 'Lokað' : 'Opinn' }}
                </span>
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                {{ new Date(record.scrapedAt).toLocaleString('is-IS') }}
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
const { getRecords } = useWaitTimeRecords()

const records = ref<any[]>([])
const loading = ref(false)
const error = ref<string | null>(null)
const filters = ref({
  restaurant: undefined as number | undefined,
  from: undefined as string | undefined,
  to: undefined as string | undefined
})

const loadRecords = async () => {
  try {
    loading.value = true
    error.value = null
    records.value = await getRecords(filters.value)
  } catch (err: any) {
    error.value = err.message || 'Villa kom upp við að sækja gögn'
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  // Load last 7 days by default
  const to = new Date()
  const from = new Date()
  from.setDate(from.getDate() - 7)
  
  filters.value.to = to.toISOString().split('T')[0]
  filters.value.from = from.toISOString().split('T')[0]
  
  loadRecords()
})
</script>
