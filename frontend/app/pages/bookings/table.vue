<template>
  <div class="w-full px-4 sm:px-6 lg:px-8 py-6">
    <div class="mb-4 flex items-center justify-between">
      <h1 class="text-2xl font-bold text-gray-900">Borðapantanir</h1>
      <div class="flex items-center gap-2">
        <button
          @click="openGreifinnIs"
          class="inline-flex items-center px-3 py-1.5 bg-indigo-600 text-white rounded-md hover:bg-indigo-700 transition-colors text-sm font-medium"
        >
          <svg class="w-4 h-4 mr-1.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
          </svg>
          Opna Greifinn.is
        </button>
        <button
          @click="openPrintPage"
          class="inline-flex items-center px-3 py-1.5 bg-gray-600 text-white rounded-md hover:bg-gray-700 transition-colors text-sm font-medium"
        >
          <svg class="w-4 h-4 mr-1.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 17h2a2 2 0 002-2v-4a2 2 0 00-2-2H5a2 2 0 00-2 2v4a2 2 0 002 2h2m2 4h6a2 2 0 002-2v-4a2 2 0 00-2-2H9a2 2 0 00-2 2v4a2 2 0 002 2zm8-12V5a2 2 0 00-2-2H9a2 2 0 00-2 2v4h10z" />
          </svg>
          Prenta
        </button>
      </div>
    </div>

    <!-- Filters -->
    <div class="mb-4 bg-white rounded-lg shadow px-4 py-3">
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-3">
        <div>
          <label class="block text-xs font-medium text-gray-600 mb-1">Dagsetning</label>
          <input
            v-model="selectedDate"
            type="date"
            class="w-full px-3 py-1.5 text-sm border border-gray-300 rounded-md focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-all"
            @change="loadBookings"
          />
        </div>
        <div>
          <label class="block text-xs font-medium text-gray-600 mb-1">Sími</label>
          <input
            v-model="filters.contactPhone"
            type="text"
            placeholder="Símanúmer"
            class="w-full px-3 py-1.5 text-sm border border-gray-300 rounded-md focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-all"
            @input="debouncedLoad"
          />
        </div>
        <div>
          <label class="block text-xs font-medium text-gray-600 mb-1">Nafn</label>
          <input
            v-model="filters.contactName"
            type="text"
            placeholder="Nafn"
            class="w-full px-3 py-1.5 text-sm border border-gray-300 rounded-md focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-all"
            @input="debouncedLoad"
          />
        </div>
        <div>
          <label class="block text-xs font-medium text-gray-600 mb-1">Staða</label>
          <select
            v-model="filters.statusId"
            class="w-full px-3 py-1.5 text-sm border border-gray-300 rounded-md focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 transition-all"
            @change="loadBookings"
          >
            <option :value="null">Allar</option>
            <option :value="0">Staðfest</option>
            <option :value="1">Fyrirspurn</option>
            <option :value="2">Afboðuð</option>
          </select>
        </div>
      </div>
    </div>

    <!-- Other Booking System Card -->
    <div class="mb-4 bg-white rounded-lg shadow overflow-hidden">
      <button
        @click="showOtherBookings = !showOtherBookings"
        class="w-full px-4 py-2.5 flex items-center justify-between hover:bg-gray-50 transition-colors"
      >
        <div class="flex items-center">
          <h2 class="text-base font-semibold text-gray-800">
            Gamli BókAri
          </h2>
          <span v-if="otherBookingsData" class="ml-3 px-2 py-1 text-xs font-medium bg-indigo-100 text-indigo-800 rounded-full">
            {{ otherBookingsData.length }}
          </span>
        </div>
        <svg
          class="w-5 h-5 text-gray-500 transition-transform"
          :class="{ 'rotate-180': showOtherBookings }"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
        </svg>
      </button>

      <div v-if="showOtherBookings" class="border-t border-gray-200">
        <div v-if="loadingOtherBookings" class="p-4 text-center">
          <div class="inline-block animate-spin rounded-full h-6 w-6 border-b-2 border-indigo-600"></div>
          <p class="mt-2 text-xs text-gray-600">Sæki bókanir...</p>
        </div>
        <div v-else-if="otherBookingsError" class="p-4 text-center">
          <p class="text-red-600 text-xs">{{ otherBookingsError }}</p>
        </div>
        <div v-else-if="otherBookingsData && otherBookingsData.length > 0" class="p-4">
          <div class="space-y-2">
            <div
              v-for="booking in otherBookingsData"
              :key="`${booking.date}-${booking.startTime}-${booking.customerName}`"
              class="flex items-center justify-between p-2 bg-gray-50 rounded-md hover:bg-gray-100 transition-colors"
            >
              <div class="flex-1">
                <div class="flex items-center gap-2">
                  <span class="text-xs font-medium text-gray-900">
                    {{ booking.startTime }}{{ booking.endTime ? ` - ${booking.endTime}` : '' }}
                  </span>
                  <span class="text-xs text-gray-700 font-medium">{{ booking.customerName }}</span>
                  <span v-if="booking.shortDescription" class="text-xs text-gray-500 italic">
                    {{ booking.shortDescription }}
                  </span>
                </div>
                <div class="mt-0.5 flex items-center gap-2 text-xs text-gray-600">
                  <span v-if="booking.adultCount > 0 || booking.childCount > 0">
                    {{ booking.adultCount }} fullorðnir{{ booking.childCount > 0 ? `, ${booking.childCount} börn` : '' }}
                  </span>
                  <span v-if="booking.locationCode" class="px-1.5 py-0.5 bg-indigo-100 text-indigo-700 rounded text-xs">
                    {{ booking.locationCode }}
                  </span>
                  <span
                    v-if="booking.status"
                    class="px-1.5 py-0.5 rounded text-xs font-medium"
                    :class="getOtherBookingStatusClass(booking.status)"
                  >
                    {{ booking.status }}
                  </span>
                </div>
              </div>
              <a
                v-if="booking.detailUrl"
                :href="booking.detailUrl"
                target="_blank"
                rel="noopener noreferrer"
                class="ml-3 text-indigo-600 hover:text-indigo-900"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
                </svg>
              </a>
            </div>
          </div>
        </div>
        <div v-else class="p-4 text-center text-xs text-gray-500">
          Engar bókanir fundust fyrir þessa dagsetningu
        </div>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="text-center py-12">
      <div class="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600"></div>
      <p class="mt-4 text-gray-600">Sæki borðapantanir...</p>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="bg-red-50 border border-red-200 rounded-lg p-6 text-center">
      <p class="text-red-800 font-semibold">Villa við að sækja borðapantanir</p>
      <p class="text-red-600 mt-2">{{ error }}</p>
      <button
        @click="loadBookings"
        class="mt-4 px-4 py-2 bg-red-600 text-white rounded-md hover:bg-red-700 transition-colors"
      >
        Reyna aftur
      </button>
    </div>

    <!-- Bookings Table -->
    <div v-else-if="bookingsData" class="bg-white rounded-lg shadow overflow-hidden">
      <div class="px-4 py-2 border-b border-gray-200 bg-gray-50">
        <div class="flex items-center justify-between">
          <h2 class="text-sm font-semibold text-gray-800">
            Borðapantanir ({{ bookingsData.totalCount }})
          </h2>
          <div class="flex items-center gap-2" v-if="bookingsData.totalCount > 0">
            <button
              @click="previousPage"
              :disabled="page === 1"
              class="px-2 py-1 text-xs border rounded hover:bg-gray-100 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              ← Fyrri
            </button>
            <span class="text-xs text-gray-700">Síða {{ page }} af {{ totalPages }}</span>
            <button
              @click="nextPage"
              :disabled="!bookingsData.hasMorePages"
              class="px-2 py-1 text-xs border rounded hover:bg-gray-100 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              Næsta →
            </button>
          </div>
        </div>
      </div>

      <div v-if="bookingsData.bookings.length === 0" class="text-center py-8 text-sm text-gray-500">
        Engar borðapantanir fundust
      </div>

      <div v-else class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200">
          <thead class="bg-gray-50">
            <tr>
              <th class="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Dagsetning & Tími
              </th>
              <th class="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Nafn
              </th>
              <th class="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Sími
              </th>
              <th class="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Gestir
              </th>
              <th class="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Staða
              </th>
              <th class="px-3 py-2 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">
                Athugasemd
              </th>
              <th class="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Aðgerðir
              </th>
            </tr>
          </thead>
          <tbody class="bg-white divide-y divide-gray-200">
            <tr
              v-for="booking in bookingsData.bookings"
              :key="booking.bookingId"
              class="hover:bg-gray-50 transition-colors"
            >
              <td class="px-3 py-2 whitespace-nowrap text-xs text-gray-900">
                <div v-if="booking.timestamp">
                  {{ formatDateTime(booking.timestamp) }}
                </div>
                <div v-else class="text-gray-400">-</div>
              </td>
              <td class="px-3 py-2 whitespace-nowrap text-xs text-gray-900">
                {{ booking.contactName || '-' }}
              </td>
              <td class="px-3 py-2 whitespace-nowrap text-xs text-gray-900">
                {{ booking.contactPhone || '-' }}
              </td>
              <td class="px-3 py-2 whitespace-nowrap text-xs text-gray-900">
                {{ booking.guestCount ?? '-' }}
              </td>
              <td class="px-3 py-2 whitespace-nowrap">
                <span
                  class="px-1.5 py-0.5 text-xs font-medium rounded-full"
                  :class="getStatusBadgeClass(booking.status)"
                >
                  {{ booking.status || 'Óþekkt' }}
                </span>
              </td>
              <td class="px-3 py-2 whitespace-nowrap text-center">
                <span v-if="booking.hasComment" class="text-indigo-600" title="Athugasemd">
                  <svg class="w-4 h-4 inline" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 8h10M7 12h4m1 8l-4-4H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-3l-4 4z" />
                  </svg>
                </span>
                <span v-else class="text-gray-300">-</span>
              </td>
              <td class="px-3 py-2 whitespace-nowrap text-xs">
                <a
                  v-if="booking.detailUrl || booking.bookingId"
                  :href="booking.detailUrl || `https://www.greifinn.is/is/bordapontun/booking/edit/${booking.bookingId}`"
                  target="_blank"
                  rel="noopener noreferrer"
                  class="text-indigo-600 hover:text-indigo-900 font-medium"
                >
                  Skoða
                </a>
                <span v-else class="text-gray-400">-</span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue';
import { useTableBookings } from '~/composables/useTableBookings';
import { useBookings } from '~/composables/useBookings';
import { useDebounceFn } from '@vueuse/core';
import type { BookingDto } from '~/types/booking';

const { getTableBookings } = useTableBookings();
const { getWeekBookings } = useBookings();

const loading = ref(false);
const error = ref<string | null>(null);
const bookingsData = ref<any>(null);

// Other booking system state
const showOtherBookings = ref(false);
const loadingOtherBookings = ref(false);
const otherBookingsError = ref<string | null>(null);
const otherBookingsData = ref<BookingDto[]>([]);

// Set default date to today
const today = new Date();
today.setHours(0, 0, 0, 0);
const selectedDate = ref<string>(today.toISOString().split('T')[0]);

const filters = ref({
  contactName: '',
  contactPhone: '',
  statusId: null as number | null
});

const page = ref(1);
const pageSize = ref(50);

const totalPages = computed(() => {
  if (!bookingsData.value) return 0;
  return Math.ceil(bookingsData.value.totalCount / pageSize.value);
});

const loadBookings = async () => {
  loading.value = true;
  error.value = null;
  
  try {
    const fromDate = new Date(selectedDate.value);
    const toDate = new Date(selectedDate.value);
    toDate.setDate(toDate.getDate() + 1); // Next day to get only the requested date
    
    // Load both table bookings and other booking system in parallel
    // Use Promise.allSettled so one failure doesn't block the other
    const [tableBookingsResult, otherBookingsResult] = await Promise.allSettled([
      getTableBookings(
        fromDate,
        toDate,
        filters.value.contactName || undefined,
        filters.value.contactPhone || undefined,
        filters.value.statusId ?? undefined,
        page.value,
        pageSize.value
      ),
      loadOtherBookings() // Always load other bookings together
    ]);
    
    // Handle table bookings result
    if (tableBookingsResult.status === 'fulfilled') {
      bookingsData.value = tableBookingsResult.value;
    } else {
      error.value = tableBookingsResult.reason?.message || 'Óþekkt villa';
      console.error('Error loading table bookings:', tableBookingsResult.reason);
    }
    
    // Other bookings errors are handled in loadOtherBookings itself
  } catch (err: any) {
    error.value = err.message || 'Óþekkt villa';
    console.error('Error loading bookings:', err);
  } finally {
    loading.value = false;
  }
};

const previousPage = () => {
  if (page.value > 1) {
    page.value--;
    loadBookings();
  }
};

const nextPage = () => {
  if (bookingsData.value?.hasMorePages) {
    page.value++;
    loadBookings();
  }
};

const formatDate = (dateString: string): string => {
  try {
    const date = new Date(dateString);
    const day = date.getDate().toString().padStart(2, '0');
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const year = date.getFullYear();
    return `${day}.${month}.${year}`;
  } catch {
    return dateString;
  }
};

const formatDateTime = (dateString: string): string => {
  try {
    const date = new Date(dateString);
    const day = date.getDate().toString().padStart(2, '0');
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const year = date.getFullYear();
    const hours = date.getHours().toString().padStart(2, '0');
    const minutes = date.getMinutes().toString().padStart(2, '0');
    return `${day}.${month}.${year} ${hours}:${minutes}`;
  } catch {
    return dateString;
  }
};

const getStatusBadgeClass = (status?: string): string => {
  if (!status) return 'bg-gray-100 text-gray-800';
  
  const statusLower = status.toLowerCase();
  // New status values: Ný/Afbókað/Situr/Farinn
  if (statusLower.includes('ný') || statusLower.includes('new')) {
    return 'bg-blue-100 text-blue-800';
  }
  if (statusLower.includes('afbókað') || statusLower.includes('cancelled')) {
    return 'bg-red-100 text-red-800';
  }
  if (statusLower.includes('situr') || statusLower.includes('seated')) {
    return 'bg-green-100 text-green-800';
  }
  if (statusLower.includes('farinn') || statusLower.includes('gone') || statusLower.includes('left')) {
    return 'bg-gray-100 text-gray-800';
  }
  // Fallback for old status values
  if (statusLower.includes('staðfest') || statusLower.includes('confirmed')) {
    return 'bg-blue-100 text-blue-800';
  }
  if (statusLower.includes('fyrirspurn') || statusLower.includes('inquiry')) {
    return 'bg-yellow-100 text-yellow-800';
  }
  return 'bg-gray-100 text-gray-800';
};

// Debounce search inputs
let debounceTimer: NodeJS.Timeout | null = null;
const debouncedLoad = () => {
  if (debounceTimer) {
    clearTimeout(debounceTimer);
  }
  debounceTimer = setTimeout(() => {
    page.value = 1; // Reset to first page when filtering
    loadBookings();
  }, 500);
};

// Build Greifinn.is URL with current filters
const buildGreifinnIsUrl = (): string => {
  const baseUrl = 'https://www.greifinn.is/is/bordapontun/booking';
  const params = new URLSearchParams();
  
  // Date filters - format as dd.MM.yyyy
  if (selectedDate.value) {
    const fromDate = new Date(selectedDate.value);
    const toDate = new Date(selectedDate.value);
    toDate.setDate(toDate.getDate() + 1); // Next day to get only the requested date
    
    const formatDate = (date: Date): string => {
      const day = date.getDate().toString().padStart(2, '0');
      const month = (date.getMonth() + 1).toString().padStart(2, '0');
      const year = date.getFullYear();
      return `${day}.${month}.${year}`;
    };
    
    params.append('flt-timestamp[from]', formatDate(fromDate));
    params.append('flt-timestamp[to]', formatDate(toDate));
  }
  
  // Contact name filter
  if (filters.value.contactName) {
    params.append('flt-contactName', filters.value.contactName);
  } else {
    params.append('flt-contactName', '');
  }
  
  // Contact phone filter
  if (filters.value.contactPhone) {
    params.append('flt-contactPhone', filters.value.contactPhone);
  } else {
    params.append('flt-contactPhone', '');
  }
  
  // Status filter
  if (filters.value.statusId !== null && filters.value.statusId !== undefined) {
    params.append('flt-status', filters.value.statusId.toString());
  } else {
    params.append('flt-status', '-1');
  }
  
  // For single day, use itemCount=-1
  if (selectedDate.value) {
    params.append('itemCount', '-1');
  }
  
  return `${baseUrl}?${params.toString()}`;
};

// Open Greifinn.is in new tab with current filters
const openGreifinnIs = () => {
  const url = buildGreifinnIsUrl();
  window.open(url, '_blank', 'noopener,noreferrer');
};

// Open print page with current date
const openPrintPage = () => {
  const printUrl = `/bookings/print?date=${selectedDate.value}`;
  window.open(printUrl, '_blank', 'noopener,noreferrer');
};

// Load other booking system bookings
const loadOtherBookings = async () => {
  if (!selectedDate.value) return;
  
  loadingOtherBookings.value = true;
  otherBookingsError.value = null;
  
  try {
    // Convert selected date to unix timestamp (start of day in UTC)
    const date = new Date(selectedDate.value);
    date.setHours(0, 0, 0, 0);
    const unixTimestamp = Math.floor(date.getTime() / 1000);
    
    const weekData = await getWeekBookings(unixTimestamp);
    
    // Find bookings for the selected date
    const selectedDateStr = selectedDate.value;
    const dayBookings = weekData.days?.find((day: any) => {
      const dayDate = new Date(day.date);
      const dayDateStr = dayDate.toISOString().split('T')[0];
      return dayDateStr === selectedDateStr;
    });
    
    if (dayBookings && dayBookings.bookings) {
      otherBookingsData.value = dayBookings.bookings;
    } else {
      otherBookingsData.value = [];
    }
  } catch (err: any) {
    otherBookingsError.value = err.message || 'Villa við að sækja bókanir';
    console.error('Error loading other bookings:', err);
    otherBookingsData.value = [];
  } finally {
    loadingOtherBookings.value = false;
  }
};

// Note: Other bookings are now loaded together with table bookings in loadBookings()
// No need to watch for date changes or card expansion separately

const getOtherBookingStatusClass = (status: string): string => {
  const statusLower = status.toLowerCase();
  if (statusLower.includes('staðfest') || statusLower.includes('confirmed')) {
    return 'bg-green-100 text-green-800';
  }
  if (statusLower.includes('fyrirspurn') || statusLower.includes('inquiry')) {
    return 'bg-yellow-100 text-yellow-800';
  }
  if (statusLower.includes('afboðuð') || statusLower.includes('cancelled')) {
    return 'bg-red-100 text-red-800';
  }
  return 'bg-gray-100 text-gray-800';
};

// Load bookings on mount
onMounted(() => {
  loadBookings();
});
</script>

