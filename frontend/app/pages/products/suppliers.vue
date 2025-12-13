<template>
  <div class="min-h-[80vh]">
    <!-- Header -->
    <div class="bg-gradient-to-br from-indigo-50 via-purple-50 to-pink-50 rounded-2xl p-8 border border-indigo-100 shadow-lg mb-6">
      <div class="flex items-center gap-3">
        <div class="w-12 h-12 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-xl flex items-center justify-center shadow-lg">
          <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
          </svg>
        </div>
        <div>
          <h1 class="text-2xl font-bold text-gray-800">Birgjar</h1>
          <p class="text-sm text-gray-600">{{ suppliers.length }} birgjar í kerfinu</p>
        </div>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="flex justify-center items-center py-20">
      <div class="relative">
        <div class="w-20 h-20 border-4 border-indigo-200 rounded-full"></div>
        <div class="w-20 h-20 border-4 border-indigo-600 border-t-transparent rounded-full animate-spin absolute top-0"></div>
      </div>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="bg-red-50 border-2 border-red-200 rounded-2xl p-8 text-center">
      <div class="w-16 h-16 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-4">
        <svg class="w-8 h-8 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
        </svg>
      </div>
      <h3 class="text-xl font-bold text-red-900 mb-2">Villa kom upp</h3>
      <p class="text-red-700">{{ error }}</p>
    </div>

    <!-- Suppliers Grid -->
    <div v-else-if="suppliers.length > 0" class="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
      <div 
        v-for="supplier in suppliers" 
        :key="supplier.id"
        class="bg-white rounded-2xl shadow-lg border border-gray-100 hover:shadow-xl hover:border-indigo-300 transition-all duration-300 overflow-hidden group"
      >
        <!-- Supplier Header -->
        <div class="bg-gradient-to-r from-indigo-500 to-purple-600 p-6">
          <div class="flex items-center gap-3">
            <div class="w-14 h-14 bg-white/20 backdrop-blur-sm rounded-xl flex items-center justify-center">
              <span class="text-2xl font-bold text-white">{{ supplier.name.substring(0, 2).toUpperCase() }}</span>
            </div>
            <div class="flex-1">
              <h3 class="text-xl font-bold text-white">{{ supplier.name }}</h3>
            </div>
          </div>
        </div>

        <!-- Supplier Info -->
        <div class="p-6 space-y-4">
          <!-- Contact Info -->
          <div v-if="supplier.contactInfo" class="flex items-start gap-3">
            <svg class="w-5 h-5 text-indigo-600 flex-shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
            </svg>
            <div>
              <p class="text-xs text-gray-500 uppercase tracking-wide">Tengiliður</p>
              <p class="text-sm text-gray-700">{{ supplier.contactInfo }}</p>
            </div>
          </div>

          <!-- Address -->
          <div v-if="supplier.address" class="flex items-start gap-3">
            <svg class="w-5 h-5 text-indigo-600 flex-shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
            </svg>
            <div>
              <p class="text-xs text-gray-500 uppercase tracking-wide">Heimilisfang</p>
              <p class="text-sm text-gray-700">{{ supplier.address }}</p>
            </div>
          </div>

          <!-- View Products Button -->
          <NuxtLink 
            :to="`/products/catalog?supplierId=${supplier.id}`"
            class="mt-4 w-full inline-flex items-center justify-center gap-2 px-4 py-3 bg-gradient-to-r from-indigo-500 to-purple-600 text-white rounded-lg hover:from-indigo-600 hover:to-purple-700 transition-all duration-200 shadow-md hover:shadow-lg font-medium"
          >
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
            </svg>
            Skoða vörur
          </NuxtLink>
        </div>
      </div>
    </div>

    <!-- Empty State -->
    <div v-else class="bg-gradient-to-br from-gray-50 to-gray-100 rounded-2xl p-12 text-center border-2 border-dashed border-gray-300">
      <div class="w-20 h-20 bg-gray-200 rounded-full flex items-center justify-center mx-auto mb-4">
        <svg class="w-10 h-10 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
        </svg>
      </div>
      <h3 class="text-xl font-bold text-gray-700 mb-2">Engir birgjar fundust</h3>
      <p class="text-gray-500">Birgjar verða sjálfkrafa búnir til þegar þú lest inn reikninga.</p>
      <NuxtLink 
        to="/products"
        class="inline-block mt-6 px-6 py-3 bg-gradient-to-r from-indigo-500 to-purple-600 text-white rounded-lg hover:from-indigo-600 hover:to-purple-700 transition-all duration-200 shadow-md hover:shadow-lg font-medium"
      >
        Lesa inn reikning
      </NuxtLink>
    </div>
  </div>
</template>

<script setup lang="ts">
const { getAllSuppliers } = useSuppliers()

const suppliers = ref<any[]>([])
const isLoading = ref(true)
const error = ref('')

const fetchSuppliers = async () => {
  isLoading.value = true
  error.value = ''
  
  try {
    suppliers.value = await getAllSuppliers()
  } catch (err: any) {
    error.value = err.message || 'Ekki tókst að sækja birgja'
    console.error(err)
  } finally {
    isLoading.value = false
  }
}

onMounted(() => {
  fetchSuppliers()
})
</script>

<style scoped>
@keyframes spin {
  from {
    transform: rotate(0deg);
  }
  to {
    transform: rotate(360deg);
  }
}
</style>
