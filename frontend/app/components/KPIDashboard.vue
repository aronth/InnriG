<template>
  <div class="space-y-6">
    <h2 class="text-2xl font-bold text-gray-800">Mælanlegir árangurspunktar (KPIs)</h2>
    
    <div v-if="loading" class="text-center py-8">
      <div class="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600"></div>
    </div>

    <div v-else-if="error" class="text-red-600 text-center py-4">
      {{ error }}
    </div>

    <div v-else-if="kpis" class="grid grid-cols-1 md:grid-cols-2 gap-6">
      <!-- KPI 1: Product List Completeness -->
      <KPICard
        title="Vörulisti til staðar"
        :current="kpis.productListCompleteness.current"
        :target="kpis.productListCompleteness.target"
        :status="kpis.productListCompleteness.status"
        :details="kpis.productListCompleteness.details"
        unit="%"
        description="100% vörulistinn til staðar fyrir alla birgja"
      />

      <!-- KPI 2: Stale Products -->
      <KPICard
        title="Vörur án nýrra reikninga"
        :current="kpis.staleProducts.current"
        :target="kpis.staleProducts.target"
        :status="kpis.staleProducts.status"
        :details="kpis.staleProducts.details"
        unit="%"
        description="Engin vöntun á vörunúmerum í bókhaldi eða vörubirgðum"
        :inverted="true"
      />

      <!-- KPI 3: Quarterly Update Frequency -->
      <KPICard
        title="Uppfærslutíðni"
        :current="kpis.quarterlyUpdateFrequency.current"
        :target="kpis.quarterlyUpdateFrequency.target"
        :status="kpis.quarterlyUpdateFrequency.status"
        :details="kpis.quarterlyUpdateFrequency.details"
        unit="%"
        description="Samanburðarlisti til innkaupa uppfærður 4x á ári"
      />

      <!-- KPI 4: Usage Metrics -->
      <KPICard
        title="Tímasparnaður"
        :current="kpis.usageMetrics.current"
        :target="kpis.usageMetrics.target"
        :status="kpis.usageMetrics.status"
        :details="kpis.usageMetrics.details"
        unit="reikningar"
        description="Tími sem fer í verðathuganir minnkar verulega"
      />

      <!-- Quarterly Breakdown -->
      <div class="md:col-span-2 bg-white rounded-lg shadow-md p-6">
        <h3 class="text-lg font-semibold mb-4">Uppfærslutíðni eftir ársfjórðungum</h3>
        <div class="grid grid-cols-4 gap-4">
          <QuarterCard
            label="Q1"
            :updated="kpis.quarterlyBreakdown.q1.updated"
            :total="kpis.quarterlyBreakdown.q1.total"
            :percentage="kpis.quarterlyBreakdown.q1.percentage"
          />
          <QuarterCard
            label="Q2"
            :updated="kpis.quarterlyBreakdown.q2.updated"
            :total="kpis.quarterlyBreakdown.q2.total"
            :percentage="kpis.quarterlyBreakdown.q2.percentage"
          />
          <QuarterCard
            label="Q3"
            :updated="kpis.quarterlyBreakdown.q3.updated"
            :total="kpis.quarterlyBreakdown.q3.total"
            :percentage="kpis.quarterlyBreakdown.q3.percentage"
          />
          <QuarterCard
            label="Q4"
            :updated="kpis.quarterlyBreakdown.q4.updated"
            :total="kpis.quarterlyBreakdown.q4.total"
            :percentage="kpis.quarterlyBreakdown.q4.percentage"
          />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { KPIsDto } from '~/app/composables/useKPIs'

const { getKPIs } = useKPIs()

const kpis = ref<KPIsDto | null>(null)
const loading = ref(true)
const error = ref<string | null>(null)

onMounted(async () => {
  try {
    loading.value = true
    error.value = null
    kpis.value = await getKPIs()
  } catch (e: any) {
    error.value = e.message || 'Villa við að sækja KPIs'
    console.error('Error fetching KPIs:', e)
  } finally {
    loading.value = false
  }
})
</script>

