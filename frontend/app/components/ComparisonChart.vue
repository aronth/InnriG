<template>
  <div class="h-64">
    <canvas ref="chartCanvas"></canvas>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch, onBeforeUnmount } from 'vue'

interface Props {
  currentLabel: string
  previousLabel: string
  currentData: Array<{ label: string; value: number }>
  previousData: Array<{ label: string; value: number }>
  type?: 'line' | 'bar'
}

const props = withDefaults(defineProps<Props>(), {
  type: 'line'
})

const chartCanvas = ref<HTMLCanvasElement | null>(null)
let chartInstance: any = null

const initChart = async () => {
  if (!chartCanvas.value) return

  // Dynamically import Chart.js
  const { Chart, registerables } = await import('chart.js')
  Chart.register(...registerables)

  const ctx = chartCanvas.value.getContext('2d')
  if (!ctx) return

  const labels = props.currentData.map(d => d.label)

  const config: any = {
    type: props.type,
    data: {
      labels,
      datasets: [
        {
          label: props.currentLabel,
          data: props.currentData.map(d => d.value),
          borderColor: 'rgb(99, 102, 241)',
          backgroundColor: props.type === 'bar' ? 'rgba(99, 102, 241, 0.5)' : 'rgba(99, 102, 241, 0.1)',
          tension: 0.4
        },
        {
          label: props.previousLabel,
          data: props.previousData.map(d => d.value),
          borderColor: 'rgb(156, 163, 175)',
          backgroundColor: props.type === 'bar' ? 'rgba(156, 163, 175, 0.5)' : 'rgba(156, 163, 175, 0.1)',
          tension: 0.4,
          borderDash: props.type === 'line' ? [5, 5] : []
        }
      ]
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: {
          position: 'top'
        },
        tooltip: {
          callbacks: {
            label: (context: any) => {
              const value = context.parsed.y
              if (props.type === 'bar' && props.currentData[0]?.value > 10000) {
                // Likely revenue
                return `${context.dataset.label}: ${new Intl.NumberFormat('is-IS', {
                  style: 'currency',
                  currency: 'ISK',
                  minimumFractionDigits: 0
                }).format(value)}`
              }
              return `${context.dataset.label}: ${value.toLocaleString('is-IS')}`
            }
          }
        }
      },
      scales: {
        y: {
          beginAtZero: true
        }
      }
    }
  }

  if (chartInstance) {
    chartInstance.destroy()
  }

  chartInstance = new Chart(ctx, config)
}

watch(() => [props.currentData, props.previousData], () => {
  initChart()
}, { deep: true })

onMounted(() => {
  initChart()
})

onBeforeUnmount(() => {
  if (chartInstance) {
    chartInstance.destroy()
  }
})
</script>

