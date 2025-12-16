<template>
  <div class="bg-white rounded-lg shadow-md p-6">
    <h3 class="text-lg font-semibold text-gray-800 mb-2">{{ title }}</h3>
    <p class="text-sm text-gray-600 mb-4">{{ description }}</p>
    
    <div class="flex items-baseline gap-2 mb-2">
      <span class="text-4xl font-bold" :class="getStatusColor()">
        {{ formatValue(current) }}{{ unit }}
      </span>
      <span v-if="target > 0" class="text-gray-500">
        / {{ formatValue(target) }}{{ unit }}
      </span>
    </div>
    
    <div class="mb-4">
      <div class="w-full bg-gray-200 rounded-full h-2.5">
        <div
          class="h-2.5 rounded-full transition-all"
          :class="getProgressBarColor()"
          :style="{ width: `${Math.min((current / (target || 1)) * 100, 100)}%` }"
        ></div>
      </div>
    </div>
    
    <div class="flex items-center justify-between">
      <span class="text-sm" :class="getStatusTextColor()">
        {{ status }}
      </span>
      <span class="text-xs text-gray-500">{{ details }}</span>
    </div>
  </div>
</template>

<script setup lang="ts">
interface Props {
  title: string
  current: number
  target: number
  status: string
  details: string
  unit: string
  description: string
  inverted?: boolean
}

const props = defineProps<Props>()

const formatValue = (value: number) => {
  return new Intl.NumberFormat('is-IS', {
    minimumFractionDigits: 1,
    maximumFractionDigits: 1
  }).format(value)
}

const getStatusColor = () => {
  if (props.status === 'Met') return 'text-green-600'
  if (props.status === 'Ekki metið') return 'text-red-600'
  return 'text-indigo-600'
}

const getProgressBarColor = () => {
  if (props.status === 'Met') return 'bg-green-600'
  if (props.status === 'Ekki metið') return 'bg-red-600'
  return 'bg-indigo-600'
}

const getStatusTextColor = () => {
  if (props.status === 'Met') return 'text-green-700 font-semibold'
  if (props.status === 'Ekki metið') return 'text-red-700 font-semibold'
  return 'text-indigo-700'
}
</script>

