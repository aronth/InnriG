<template>
  <div class="bg-white rounded-xl shadow-md border border-gray-100 p-6">
    <p class="text-sm text-gray-500 mb-2">{{ title }}</p>
    <div class="flex items-baseline justify-between">
      <p class="text-3xl font-bold text-gray-900">
        {{ formatValue(current, format) }}
      </p>
      <div v-if="previous !== null && previous !== undefined" class="flex items-center gap-1">
        <span :class="getChangeColor(change, inverse)" class="text-sm font-medium">
          {{ formatChange(change) }}
        </span>
        <span :class="getChangeIconColor(change, inverse)" class="text-lg">
          {{ getChangeIcon(change) }}
        </span>
      </div>
    </div>
    <p v-if="previous !== null && previous !== undefined" class="text-xs text-gray-400 mt-2">
      Fyrri: {{ formatValue(previous, format) }}
    </p>
  </div>
</template>

<script setup lang="ts">
interface Props {
  title: string
  current: number | null | undefined
  previous: number | null | undefined
  change?: number | null
  format?: 'number' | 'currency' | 'percent' | 'minutes'
  inverse?: boolean // If true, negative change is good (e.g., for late ratio, wait time)
}

const props = withDefaults(defineProps<Props>(), {
  format: 'number',
  inverse: false
})

const formatValue = (value: number | null | undefined, format: string) => {
  if (value === null || value === undefined) return '-'
  
  switch (format) {
    case 'currency':
      return new Intl.NumberFormat('is-IS', {
        style: 'currency',
        currency: 'ISK',
        minimumFractionDigits: 0,
        maximumFractionDigits: 0
      }).format(value)
    case 'percent':
      return `${value.toFixed(1)}%`
    case 'minutes':
      return `${Math.round(value)} mín`
    default:
      return value.toLocaleString('is-IS')
  }
}

const formatChange = (change: number | null | undefined) => {
  if (change === null || change === undefined) return ''
  const sign = change >= 0 ? '+' : ''
  return `${sign}${change.toFixed(1)}%`
}

const getChangeColor = (change: number | null | undefined, inverse: boolean) => {
  if (change === null || change === undefined) return 'text-gray-500'
  const isGood = inverse ? change < 0 : change > 0
  return isGood ? 'text-green-600' : change < 0 ? 'text-red-600' : 'text-gray-500'
}

const getChangeIconColor = (change: number | null | undefined, inverse: boolean) => {
  if (change === null || change === undefined) return 'text-gray-500'
  const isGood = inverse ? change < 0 : change > 0
  return isGood ? 'text-green-600' : change < 0 ? 'text-red-600' : 'text-gray-500'
}

const getChangeIcon = (change: number | null | undefined) => {
  if (change === null || change === undefined) return '→'
  if (change > 0) return '↑'
  if (change < 0) return '↓'
  return '→'
}
</script>
