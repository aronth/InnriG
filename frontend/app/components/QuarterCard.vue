<template>
  <div class="bg-white border rounded-lg p-4">
    <div class="text-sm font-medium text-gray-700 mb-2">{{ label }}</div>
    <div class="text-2xl font-bold text-gray-900 mb-1">
      {{ updated }} / {{ total }}
    </div>
    <div class="text-sm text-gray-600">
      {{ formatPercent(percentage) }}
    </div>
    <div class="mt-2 w-full bg-gray-200 rounded-full h-2">
      <div
        class="h-2 rounded-full transition-all"
        :class="getProgressColor()"
        :style="{ width: `${Math.min(percentage, 100)}%` }"
      ></div>
    </div>
  </div>
</template>

<script setup lang="ts">
interface Props {
  label: string
  updated: number
  total: number
  percentage: number
}

const props = defineProps<Props>()

const formatPercent = (value: number) => {
  return new Intl.NumberFormat('is-IS', {
    minimumFractionDigits: 1,
    maximumFractionDigits: 1
  }).format(value) + '%'
}

const getProgressColor = () => {
  if (props.percentage >= 100) return 'bg-green-600'
  if (props.percentage >= 75) return 'bg-yellow-500'
  return 'bg-red-500'
}
</script>

