<template>
  <div class="bg-white rounded-xl shadow-sm border border-gray-200">
    <!-- Header -->
    <div class="p-4 border-b border-gray-200">
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-4">
          <div class="w-12 h-12 bg-gradient-to-br from-blue-500 to-blue-600 rounded-lg flex items-center justify-center shadow-md">
            <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
            </svg>
          </div>
          <div>
            <h3 class="text-lg font-semibold text-gray-900">Bókanir vikunnar</h3>
            <p v-if="!loading && weekData" class="text-sm text-gray-600">
              {{ formatWeekRange() }}
            </p>
          </div>
        </div>
        <div class="flex items-center gap-2">
          <button
            @click="handleClearCache"
            :disabled="clearingCache"
            class="px-3 py-2 text-sm bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200 transition-colors font-medium flex items-center gap-1 disabled:opacity-50 disabled:cursor-not-allowed"
            title="Hreinsa skyndiminni"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
            </svg>
            <span v-if="!clearingCache">Hreinsa</span>
            <span v-else class="animate-pulse">...</span>
          </button>
          <button
            @click="isExpanded = !isExpanded"
            class="px-4 py-2 text-sm bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200 transition-colors font-medium flex items-center gap-2"
          >
            {{ isExpanded ? 'Fela' : 'Sýna allt' }}
            <svg
              class="w-4 h-4 transition-transform"
              :class="{ 'rotate-180': isExpanded }"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
            </svg>
          </button>
          <NuxtLink
            to="/bookings"
            class="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors font-medium text-sm"
          >
            Opna bókanir
          </NuxtLink>
        </div>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="p-6 text-center">
      <div class="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600"></div>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="p-4 text-center text-red-600 text-sm">
      {{ error }}
    </div>

    <!-- Week Overview -->
    <div v-else-if="weekData" class="p-4">
      <!-- Compact Week Bar -->
      <div class="grid grid-cols-7 gap-2">
        <div
          v-for="day in weekData.days"
          :key="day.date"
          :class="[
            'p-2 rounded-lg border-2 transition-all cursor-pointer',
            isToday(day.date)
              ? 'bg-indigo-50 border-indigo-400 shadow-md'
              : 'bg-gray-50 border-gray-200 hover:border-indigo-300'
          ]"
          @click="toggleDayExpansion(day.date)"
        >
          <div class="flex items-center justify-between mb-1">
            <span class="text-xs text-gray-600 uppercase font-medium">{{ getDayShortName(day.date) }}</span>
            <span class="text-lg font-bold" :class="isToday(day.date) ? 'text-indigo-600' : 'text-gray-900'">
              {{ getDayNumber(day.date) }}
            </span>
          </div>
          <div class="text-xs font-semibold text-center" :class="isToday(day.date) ? 'text-indigo-600' : 'text-gray-500'">
            {{ day.bookings.length }} {{ day.bookings.length === 1 ? 'Bókun' : 'Bókanir' }}
          </div>
        </div>
      </div>

      <!-- Expanded View -->
      <div v-if="isExpanded" class="space-y-4 mt-4 pt-4 border-t border-gray-200">
        <div v-for="day in weekData.days" :key="day.date">
          <div
            v-if="day.bookings.length > 0"
            class="mb-4"
          >
            <h4 class="text-sm font-semibold text-gray-700 mb-2 flex items-center gap-2">
              <span>{{ day.dayName }}</span>
              <span v-if="isToday(day.date)" class="text-xs bg-indigo-100 text-indigo-700 px-2 py-0.5 rounded">
                Í dag
              </span>
            </h4>
            <div class="space-y-2">
              <a
                v-for="booking in day.bookings"
                :key="booking.detailUrl"
                :href="booking.detailUrl"
                target="_blank"
                rel="noopener noreferrer"
                class="block border rounded-lg p-3 hover:shadow-md transition-shadow text-sm"
                :class="getBookingStatusClass(booking.status)"
              >
                <div class="flex items-center justify-between">
                  <div class="flex items-center gap-3 flex-1">
                    <span class="font-semibold text-gray-900">{{ booking.startTime }}</span>
                    <span class="text-gray-600 truncate">{{ booking.customerName || booking.shortDescription }}</span>
                  </div>
                  <div class="flex items-center gap-2">
                    <span v-if="booking.locationCode" class="text-xs text-gray-500">
                      {{ booking.locationCode }}
                    </span>
                    <span class="text-xs font-semibold text-gray-700">
                      {{ booking.adultCount }} gest{{ booking.adultCount === 1 ? 'ur' : 'ir' }}
                    </span>
                  </div>
                </div>
              </a>
            </div>
          </div>
        </div>
      </div>

      <!-- Individual Day Expansion (when clicking on a day in compact view) -->
      <div v-else-if="expandedDay" class="mt-4 pt-4 border-t border-gray-200">
        <div class="mb-4">
          <div class="flex items-center justify-between mb-3">
            <h4 class="text-sm font-semibold text-gray-700 flex items-center gap-2">
              <span>{{ getDayByDate(expandedDay)?.dayName }}</span>
              <span v-if="isToday(expandedDay)" class="text-xs bg-indigo-100 text-indigo-700 px-2 py-0.5 rounded">
                Í dag
              </span>
            </h4>
            <button
              @click="expandedDay = null"
              class="text-xs text-gray-500 hover:text-gray-700"
            >
              Loka
            </button>
          </div>
          <div v-if="getDayByDate(expandedDay)?.bookings.length === 0" class="text-center text-gray-500 py-4 text-sm">
            Engar bókanir
          </div>
          <div v-else class="space-y-2">
            <a
              v-for="booking in getDayByDate(expandedDay)?.bookings"
              :key="booking.detailUrl"
              :href="booking.detailUrl"
              target="_blank"
              rel="noopener noreferrer"
              class="block border rounded-lg p-3 hover:shadow-md transition-shadow text-sm"
              :class="getBookingStatusClass(booking.status)"
            >
              <div class="flex items-center justify-between">
                <div class="flex items-center gap-3 flex-1">
                  <span class="font-semibold text-gray-900">{{ booking.startTime }}</span>
                  <span class="text-gray-600 truncate">{{ booking.customerName || booking.shortDescription }}</span>
                </div>
                <div class="flex items-center gap-2">
                  <span v-if="booking.locationCode" class="text-xs text-gray-500">
                    {{ booking.locationCode }}
                  </span>
                  <span class="text-xs font-semibold text-gray-700">
                    {{ booking.adultCount }} gest{{ booking.adultCount === 1 ? 'ur' : 'ir' }}
                  </span>
                </div>
              </div>
            </a>
          </div>
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
const isExpanded = ref(false);
const expandedDay = ref<string | null>(null);
const clearingCache = ref(false);

const loadBookings = async () => {
  loading.value = true;
  error.value = null;
  
  try {
    const timestamp = Math.floor(Date.now() / 1000);
    weekData.value = await getWeekBookings(timestamp);
  } catch (err: any) {
    error.value = err.message || 'Villa við að sækja bókanir';
    console.error('Error loading bookings:', err);
  } finally {
    loading.value = false;
  }
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

const isToday = (dateStr: string) => {
  const today = new Date().toISOString().split('T')[0];
  const checkDate = new Date(dateStr).toISOString().split('T')[0];
  return today === checkDate;
};

const getDayShortName = (dateStr: string) => {
  // Use consistent 3-letter abbreviations based on day of week
  const date = new Date(dateStr);
  const dayOfWeek = date.getDay();
  const dayNames = ['Sun', 'Mán', 'Þri', 'Mið', 'Fim', 'Fös', 'Lau'];
  return dayNames[dayOfWeek];
};

const getDayNumber = (dateStr: string) => {
  const date = new Date(dateStr);
  return date.getDate();
};

const formatWeekRange = () => {
  if (!weekData.value) return '';
  
  const start = new Date(weekData.value.weekStart);
  const end = new Date(weekData.value.weekEnd);
  
  const formatter = new Intl.DateTimeFormat('is-IS', {
    day: 'numeric',
    month: 'long'
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

const toggleDayExpansion = (dateStr: string) => {
  if (isExpanded.value) return; // Don't toggle individual days when fully expanded
  
  if (expandedDay.value === dateStr) {
    expandedDay.value = null;
  } else {
    expandedDay.value = dateStr;
  }
};

const getDayByDate = (dateStr: string | null) => {
  if (!dateStr || !weekData.value) return null;
  return weekData.value.days.find((day: any) => day.date === dateStr);
};

// Load bookings on mount
onMounted(() => {
  loadBookings();
});
</script>

