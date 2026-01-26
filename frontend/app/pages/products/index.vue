<template>
  <div class="space-y-6">
    <!-- KPI Dashboard -->
    <KPIDashboard />

    <!-- Supplier Update Status Panel - Foldable -->
    <div class="bg-white rounded-lg shadow-md overflow-hidden">
      <button
        @click="isSupplierPanelOpen = !isSupplierPanelOpen"
        class="w-full flex items-center justify-between p-6 hover:bg-gray-50 transition-colors"
      >
        <div class="flex items-center gap-3">
          <h2 class="text-xl font-bold text-gray-800">
            Tíðni uppfærslna birgja
          </h2>
          <div class="text-sm text-gray-500">
            Markmið: 4x á ári (1x á hvert ársfjórðung)
          </div>
        </div>
        <svg
          class="w-5 h-5 text-gray-500 transition-transform duration-200"
          :class="{ 'rotate-180': isSupplierPanelOpen }"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
        </svg>
      </button>
      
      <div v-show="isSupplierPanelOpen" class="border-t border-gray-200">
        <div class="supplier-panel-content">
          <SupplierUpdateStatusPanel />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
const { getCurrentUser } = useAuth()

// Supplier panel is folded by default
const isSupplierPanelOpen = ref(false)

// Ensure user is loaded on mount
onMounted(async () => {
  await getCurrentUser()
})
</script>

<style scoped>
.supplier-panel-content :deep(.bg-white) {
  background: transparent;
  box-shadow: none;
  padding: 0;
}

.supplier-panel-content :deep(.flex.items-center.justify-between.mb-4) {
  display: none;
}
</style>
