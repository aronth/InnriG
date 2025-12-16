<template>
  <div class="space-y-6">
    <!-- Welcome Section -->
    <div class="bg-gradient-to-r from-indigo-600 to-indigo-800 rounded-lg p-6 text-white shadow-md">
      <h1 class="text-2xl font-bold mb-1">Velkomin í InnriGreifann</h1>
      <p class="text-indigo-100">
        Miðstöð fyrir innri aðgerðir, reikningavinnslu og fjárhagsgreiningu.
      </p>
    </div>

    <!-- KPI Dashboard -->
    <KPIDashboard />

    <!-- Supplier Update Status Panel -->
    <SupplierUpdateStatusPanel />

    <!-- Dashboard Grid -->
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <!-- Price Comparison Report -->
      <div class="lg:col-span-2">
        <PriceComparisonReport />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
const { getCurrentUser } = useAuth()

// Ensure user is loaded on mount
onMounted(async () => {
  await getCurrentUser()
})
</script>
