<template>
  <div class="w-full px-4 sm:px-6 lg:px-8 py-8">
    <div class="mb-8">
      <h1 class="text-3xl font-bold text-gray-900">Bókanir</h1>
      <p class="mt-2 text-sm text-gray-700">
        Vikulegt yfirlit pantana frá bókunarkerfi
      </p>
    </div>

    <!-- Week Navigation -->
    <div class="mb-6 flex items-center justify-between bg-white rounded-lg shadow px-6 py-4">
      <button
        @click="goToPreviousWeek"
        class="px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700 transition-colors"
      >
        ← Fyrri vika
      </button>
      
      <div class="text-center">
        <h2 class="text-xl font-semibold text-gray-900">
          {{ formatWeekRange() }}
        </h2>
      </div>
      
      <div class="flex gap-2">
        <button
          @click="handleClearCache"
          :disabled="clearingCache"
          class="px-4 py-2 bg-gray-100 text-gray-700 rounded-md hover:bg-gray-200 transition-colors flex items-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed"
          title="Hreinsa skyndiminni"
        >
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
          </svg>
          <span v-if="!clearingCache">Hreinsa skyndiminni</span>
          <span v-else class="animate-pulse">Hreinsa...</span>
        </button>
        <button
          @click="goToCurrentWeek"
          class="px-4 py-2 bg-gray-600 text-white rounded-md hover:bg-gray-700 transition-colors"
        >
          Núverandi vika
        </button>
        <button
          @click="goToNextWeek"
          class="px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700 transition-colors"
        >
          Næsta vika →
        </button>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="text-center py-12">
      <div class="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600"></div>
      <p class="mt-4 text-gray-600">Sæki bókanir...</p>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="bg-red-50 border border-red-200 rounded-lg p-6 text-center">
      <p class="text-red-800 font-semibold">Villa við að sækja bókanir</p>
      <p class="text-red-600 mt-2">{{ error }}</p>
      <button
        @click="loadBookings"
        class="mt-4 px-4 py-2 bg-red-600 text-white rounded-md hover:bg-red-700 transition-colors"
      >
        Reyna aftur
      </button>
    </div>

    <!-- Week View -->
    <div v-else-if="weekData" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 xl:grid-cols-7 gap-4">
      <div
        v-for="day in weekData.days"
        :key="day.date"
        class="bg-white rounded-lg shadow-md overflow-hidden"
      >
        <!-- Day Header -->
        <div class="bg-indigo-600 text-white px-4 py-3">
          <h3 class="font-semibold text-center">{{ day.dayName }}</h3>
        </div>

        <!-- Bookings List -->
        <div class="p-4 space-y-3">
          <div
            v-if="day.bookings.length === 0"
            class="text-center text-gray-500 py-8 text-sm"
          >
            Engar bókanir
          </div>

          <a
            v-for="booking in day.bookings"
            :key="booking.detailUrl"
            :href="booking.detailUrl"
            target="_blank"
            rel="noopener noreferrer"
            class="block border rounded-lg p-3 hover:shadow-lg transition-shadow"
            :class="getBookingStatusClass(booking.status)"
          >
            <!-- Time and Print Indicator -->
            <div class="flex items-center justify-between mb-2">
              <span class="font-semibold text-gray-900">{{ booking.startTime }}</span>
              <span v-if="booking.needsPrint" class="text-xs bg-yellow-100 text-yellow-800 px-2 py-1 rounded">
                P
              </span>
            </div>

            <!-- Location Code -->
            <div v-if="booking.locationCode" class="text-xs text-gray-600 mb-1">
              {{ booking.locationCode }}
            </div>

            <!-- Customer Name -->
            <div class="font-medium text-gray-800 mb-1 truncate" :title="booking.customerName">
              {{ booking.customerName || booking.shortDescription }}
            </div>

            <!-- Guest Count -->
            <div class="text-sm text-gray-600">
              <span v-if="booking.adultCount > 0">
                {{ booking.adultCount }} {{ booking.adultCount === 1 ? 'gestur' : 'gestir' }}
              </span>
              <span v-if="booking.childCount > 0" class="ml-2">
                ({{ booking.childCount }} {{ booking.childCount === 1 ? 'barn' : 'börn' }})
              </span>
            </div>

            <!-- Status Badge -->
            <div class="mt-2">
              <span class="text-xs px-2 py-1 rounded" :class="getStatusBadgeClass(booking.status)">
                {{ booking.status }}
              </span>
            </div>
          </a>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
const { getWeekBookings, clearCache } = useBookings();

const loading = ref(false);
const error = ref<string | null>(null);
const weekData = ref<any>(null);
const currentTimestamp = ref<number>(Math.floor(Date.now() / 1000));
const clearingCache = ref(false);

const loadBookings = async () => {
  loading.value = true;
  error.value = null;
  
  try {
    weekData.value = await getWeekBookings(currentTimestamp.value);
  } catch (err: any) {
    error.value = err.message || 'Óþekkt villa';
    console.error('Error loading bookings:', err);
  } finally {
    loading.value = false;
  }
};

const goToPreviousWeek = () => {
  // Go back 7 days (in seconds)
  currentTimestamp.value -= 7 * 24 * 60 * 60;
  loadBookings();
};

const goToNextWeek = () => {
  // Go forward 7 days (in seconds)
  currentTimestamp.value += 7 * 24 * 60 * 60;
  loadBookings();
};

const goToCurrentWeek = () => {
  currentTimestamp.value = Math.floor(Date.now() / 1000);
  loadBookings();
};

const handleClearCache = async () => {
  clearingCache.value = true;
  
  try {
    await clearCache();
    // Reload bookings after clearing cache
    await loadBookings();
  } catch (err: any) {
    error.value = 'Villa við að hreinsa skyndiminni';
    console.error('Error clearing cache:', err);
  } finally {
    clearingCache.value = false;
  }
};

const formatWeekRange = () => {
  if (!weekData.value) return '';
  
  const start = new Date(weekData.value.weekStart);
  const end = new Date(weekData.value.weekEnd);
  
  const formatter = new Intl.DateTimeFormat('is-IS', {
    day: 'numeric',
    month: 'long',
    year: 'numeric'
  });
  
  return `${formatter.format(start)} - ${formatter.format(end)}`;
};

const getBookingStatusClass = (status: string) => {
  switch (status.toLowerCase()) {
    case 'staðfest':
      return 'border-blue-300 bg-blue-50';
    case 'fyrirspurn':
      return 'border-yellow-300 bg-yellow-50';
    case 'afboðuð':
      return 'border-red-300 bg-red-50';
    default:
      return 'border-gray-300 bg-gray-50';
  }
};

const getStatusBadgeClass = (status: string) => {
  switch (status.toLowerCase()) {
    case 'staðfest':
      return 'bg-blue-100 text-blue-800';
    case 'fyrirspurn':
      return 'bg-yellow-100 text-yellow-800';
    case 'afboðuð':
      return 'bg-red-100 text-red-800';
    default:
      return 'bg-gray-100 text-gray-800';
  }
};

// Load bookings on mount
onMounted(() => {
  loadBookings();
});
</script>

