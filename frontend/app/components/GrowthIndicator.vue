<template>
  <span
    v-if="value !== null && value !== undefined"
    class="text-xs font-medium"
    :class="getColorClass(value)"
    :title="`${value >= 0 ? '+' : ''}${value.toFixed(1)}%`"
  >
    {{ formatValue(value) }}
  </span>
</template>

<script setup lang="ts">
const props = defineProps<{
  value?: number | null
  reversed?: boolean // When true, higher values are worse (e.g., late ratio)
}>()

const formatValue = (val: number): string => {
  const sign = val >= 0 ? '+' : ''
  return `${sign}${val.toFixed(1)}%`
}

const getColorClass = (val: number): string => {
  // If reversed, invert the logic: positive growth is bad (red), negative is good (green)
  if (props.reversed) {
    if (val > 0) return 'text-red-600' // Increase is bad
    if (val < 0) return 'text-green-600' // Decrease is good
    return 'text-gray-500'
  }
  // Normal logic: positive growth is good (green), negative is bad (red)
  if (val > 0) return 'text-green-600'
  if (val < 0) return 'text-red-600'
  return 'text-gray-500'
}
</script>

