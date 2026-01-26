<template>
  <div class="h-96">
    <canvas ref="chartCanvas"></canvas>
  </div>
</template>

<script setup lang="ts">
import type { OrderTimelineReportDto } from '~/composables/useOrders'

interface Props {
  timelineData: OrderTimelineReportDto
}

const props = defineProps<Props>()

const chartCanvas = ref<HTMLCanvasElement | null>(null)
let chartInstance: any = null

const initChart = async () => {
  if (!chartCanvas.value || !props.timelineData) return

  // Dynamically import Chart.js and date adapter
  const { Chart, registerables } = await import('chart.js')
  await import('chartjs-adapter-date-fns')
  Chart.register(...registerables)

  const ctx = chartCanvas.value.getContext('2d')
  if (!ctx) return

  // Prepare data - filter out Salur and unknown delivery methods
  const orders = props.timelineData.orders

  // Filter to only include Sótt (pickup) and Sent (delivery)
  const filteredOrders = orders.filter(order => 
    order.deliveryMethod === 'Sótt' || order.deliveryMethod === 'Sent'
  )

  // Sort by order time
  const sortedOrders = [...filteredOrders].sort((a, b) => 
    new Date(a.orderTime).getTime() - new Date(b.orderTime).getTime()
  )

  // Create datasets - one point per order
  const waitTimeData = sortedOrders.map(order => ({
    x: new Date(order.orderTime),
    y: order.waitTimeMinutes
  }))

  const scanTimeData = sortedOrders.map(order => ({
    x: new Date(order.orderTime),
    y: order.timeToScanMinutes
  }))

  const checkoutTimeData = sortedOrders.map(order => ({
    x: new Date(order.orderTime),
    y: order.timeToCheckoutMinutes
  }))

  const config: any = {
    type: 'line',
    data: {
      datasets: [
        {
          label: 'Biðtími (áætlaður)',
          data: waitTimeData,
          borderColor: 'rgb(99, 102, 241)',
          backgroundColor: 'rgba(99, 102, 241, 0.1)',
          pointBackgroundColor: 'rgb(99, 102, 241)',
          pointBorderColor: '#fff',
          pointHoverBackgroundColor: '#fff',
          pointHoverBorderColor: 'rgb(99, 102, 241)',
          tension: 0.1,
          borderWidth: 2,
          pointRadius: 4,
          pointHoverRadius: 6,
          spanGaps: true
        },
        {
          label: 'Tími þar til skannað',
          data: scanTimeData,
          borderColor: 'rgb(16, 185, 129)',
          backgroundColor: 'rgba(16, 185, 129, 0.1)',
          pointBackgroundColor: 'rgb(16, 185, 129)',
          pointBorderColor: '#fff',
          pointHoverBackgroundColor: '#fff',
          pointHoverBorderColor: 'rgb(16, 185, 129)',
          tension: 0.1,
          borderWidth: 2,
          pointRadius: 4,
          pointHoverRadius: 6,
          spanGaps: true
        },
        {
          label: 'Tími þar til útskráð (sendar pantanir)',
          data: checkoutTimeData,
          borderColor: 'rgb(249, 115, 22)',
          backgroundColor: 'rgba(249, 115, 22, 0.1)',
          pointBackgroundColor: 'rgb(249, 115, 22)',
          pointBorderColor: '#fff',
          pointHoverBackgroundColor: '#fff',
          pointHoverBorderColor: 'rgb(249, 115, 22)',
          tension: 0.1,
          borderWidth: 2,
          pointRadius: 4,
          pointHoverRadius: 6,
          spanGaps: true
        }
      ]
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      interaction: {
        mode: 'nearest',
        axis: 'x',
        intersect: false
      },
      plugins: {
        legend: {
          position: 'top',
          labels: {
            usePointStyle: true,
            padding: 15
          }
        },
        tooltip: {
          callbacks: {
            title: (context: any) => {
              const date = context[0].parsed.x
              return new Date(date).toLocaleTimeString('is-IS', { 
                hour: '2-digit', 
                minute: '2-digit' 
              })
            },
            label: (context: any) => {
              const value = context.parsed.y
              if (value === null || value === undefined) {
                return `${context.dataset.label}: -`
              }
              return `${context.dataset.label}: ${Math.round(value)} mín`
            }
          }
        }
      },
      scales: {
        x: {
          type: 'time',
          time: {
            unit: 'hour',
            displayFormats: {
              hour: 'HH:mm'
            },
            tooltipFormat: 'HH:mm'
          },
          title: {
            display: true,
            text: 'Tími pöntunar'
          },
          grid: {
            display: true,
            color: 'rgba(0, 0, 0, 0.05)'
          }
        },
        y: {
          beginAtZero: true,
          title: {
            display: true,
            text: 'Mínútur'
          },
          grid: {
            display: true,
            color: 'rgba(0, 0, 0, 0.05)'
          },
          ticks: {
            callback: function(value: any) {
              return value + ' mín'
            }
          }
        }
      }
    }
  }

  if (chartInstance) {
    chartInstance.destroy()
  }

  chartInstance = new Chart(ctx, config)
}

watch(() => props.timelineData, () => {
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

