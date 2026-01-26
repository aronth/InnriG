<template>
  <div class="overflow-x-auto">
    <table class="min-w-full divide-y divide-gray-200">
      <thead class="bg-gray-50">
        <tr>
          <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
            {{ firstColumnLabel }}
          </th>
          <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
            Núverandi
          </th>
          <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
            Fyrri
          </th>
          <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
            Breyting
          </th>
        </tr>
      </thead>
      <tbody class="bg-white divide-y divide-gray-200">
        <tr v-for="(row, index) in currentData" :key="index" class="hover:bg-gray-50">
          <td class="px-4 py-3 whitespace-nowrap text-sm font-medium text-gray-900">
            {{ row.label }}
          </td>
          <td class="px-4 py-3 whitespace-nowrap text-sm text-right text-gray-900">
            {{ formatValue(row.orders || row.revenue || row.lateRatio) }}
            <span v-if="row.revenue" class="text-xs text-gray-500 block">
              {{ formatCurrency(row.revenue) }}
            </span>
          </td>
          <td class="px-4 py-3 whitespace-nowrap text-sm text-right text-gray-500">
            {{ formatValue(previousData[index]?.orders || previousData[index]?.revenue || previousData[index]?.lateRatio) }}
            <span v-if="previousData[index]?.revenue" class="text-xs text-gray-400 block">
              {{ formatCurrency(previousData[index].revenue) }}
            </span>
          </td>
          <td class="px-4 py-3 whitespace-nowrap text-sm text-right">
            <span :class="getChangeColor(row.change || calculateChange(row, previousData[index]))" class="font-medium">
              {{ formatChange(row.change || calculateChange(row, previousData[index])) }}
            </span>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<script setup lang="ts">
interface Props {
  currentData: Array<{
    label: string
    orders?: number
    revenue?: number
    lateRatio?: number
    change?: number
  }>
  previousData: Array<{
    label: string
    orders?: number
    revenue?: number
    lateRatio?: number
  }>
  firstColumnLabel?: string
}

const props = withDefaults(defineProps<Props>(), {
  firstColumnLabel: 'Flokkur'
})

const formatValue = (value: number | undefined) => {
  if (value === undefined || value === null) return '-'
  if (value < 1 && value > 0) {
    // Likely a ratio
    return `${(value * 100).toFixed(1)}%`
  }
  return value.toLocaleString('is-IS')
}

const formatCurrency = (value: number | undefined) => {
  if (value === undefined || value === null) return ''
  return new Intl.NumberFormat('is-IS', {
    style: 'currency',
    currency: 'ISK',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0
  }).format(value)
}

const formatChange = (change: number | undefined) => {
  if (change === undefined || change === null) return '-'
  const sign = change >= 0 ? '+' : ''
  return `${sign}${change.toFixed(1)}%`
}

const calculateChange = (current: any, previous: any) => {
  if (!previous) return undefined
  const currentVal = current.orders || current.revenue || current.lateRatio
  const previousVal = previous.orders || previous.revenue || previous.lateRatio
  if (previousVal === 0 || previousVal === undefined) return undefined
  return ((currentVal - previousVal) / previousVal) * 100
}

const getChangeColor = (change: number | undefined) => {
  if (change === undefined || change === null) return 'text-gray-500'
  if (change > 0) return 'text-green-600'
  if (change < 0) return 'text-red-600'
  return 'text-gray-500'
}
</script>

