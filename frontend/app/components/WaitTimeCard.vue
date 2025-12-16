<template>
  <div class="bg-white rounded-lg shadow p-6">
    <div class="flex justify-between items-start mb-4">
      <h3 class="text-xl font-bold text-gray-800">{{ record.restaurantName }}</h3>
      <span
        :class="[
          'px-3 py-1 rounded-full text-xs font-medium',
          record.isClosed
            ? 'bg-red-100 text-red-800'
            : 'bg-green-100 text-green-800'
        ]"
      >
        {{ record.isClosed ? 'Lokað' : 'Opinn' }}
      </span>
    </div>

    <div v-if="record.isClosed" class="text-center py-4 text-gray-500">
      <p>Veitingastaðurinn er lokaður</p>
    </div>

    <div v-else class="space-y-4">
      <div v-if="record.sottMinutes !== null && record.sottMinutes !== undefined">
        <div class="flex justify-between items-center">
          <span class="text-gray-600">Sótt biðtími:</span>
          <span class="text-2xl font-bold text-indigo-600">{{ record.sottMinutes }} mín</span>
        </div>
      </div>

      <div v-if="record.sentMinutes !== null && record.sentMinutes !== undefined">
        <div class="flex justify-between items-center">
          <span class="text-gray-600">Sent/Heimsent biðtími:</span>
          <span class="text-2xl font-bold text-indigo-600">{{ record.sentMinutes }} mín</span>
        </div>
      </div>

      <div v-if="!record.sottMinutes && !record.sentMinutes" class="text-center py-4 text-gray-500">
        <p>Engin biðtími tiltækur</p>
      </div>
    </div>

    <div class="mt-4 pt-4 border-t border-gray-200">
      <p class="text-xs text-gray-500">
        Síðast uppfært: {{ formatDate(record.scrapedAt) }}
      </p>
    </div>
  </div>
</template>

<script setup lang="ts">
interface Props {
  record: {
    id: string
    restaurant: number
    restaurantName: string
    sottMinutes?: number
    sentMinutes?: number
    isClosed: boolean
    scrapedAt: string
  }
}

const props = defineProps<Props>()

const formatDate = (dateString: string) => {
  return new Date(dateString).toLocaleString('is-IS')
}
</script>
