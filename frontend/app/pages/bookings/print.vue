<template>
  <div class="print-container">
    <div class="print-header">
      <h1 class="print-title">Borðapantanir - {{ formatDate(selectedDate) }}</h1>
    </div>

    <!-- Gamli BókAri Section (Folded/Collapsed) -->
    <div class="print-section mb-4">
      <div class="print-section-header">
        <h2 class="print-section-title">Gamli BókAri</h2>
        <span v-if="otherBookingsData && otherBookingsData.length > 0" class="print-badge">
          {{ otherBookingsData.length }}
        </span>
      </div>
      <div v-if="otherBookingsData && otherBookingsData.length > 0" class="print-other-bookings">
        <div
          v-for="booking in otherBookingsData"
          :key="`${booking.date}-${booking.startTime}-${booking.customerName}`"
          class="print-other-booking-item"
        >
          <span class="print-time">{{ booking.startTime }}{{ booking.endTime ? ` - ${booking.endTime}` : '' }}</span>
          <span class="print-name">{{ booking.customerName }}</span>
          <span v-if="booking.shortDescription" class="print-description">{{ booking.shortDescription }}</span>
          <span v-if="booking.adultCount > 0 || booking.childCount > 0" class="print-guests">
            {{ booking.adultCount }} fullorðnir{{ booking.childCount > 0 ? `, ${booking.childCount} börn` : '' }}
          </span>
        </div>
      </div>
      <div v-else class="print-empty">
        Engar bókanir
      </div>
    </div>

    <!-- Bookings Table -->
    <div v-if="filteredBookings.length > 0" class="print-table-container">
      <table class="print-table">
        <thead>
          <tr>
            <th class="print-th">Tími</th>
            <th class="print-th">Nafn</th>
            <th class="print-th">Sími</th>
            <th class="print-th">Gestir</th>
            <th class="print-th">Borð</th>
            <th class="print-th">Athugasemd</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="booking in filteredBookings"
            :key="booking.bookingId"
            class="print-tr"
          >
            <td class="print-td">
              <div v-if="booking.timestamp">
                {{ formatTime(booking.timestamp) }}
              </div>
              <div v-else class="print-empty-cell">-</div>
            </td>
            <td class="print-td">{{ booking.contactName || '-' }}</td>
            <td class="print-td">{{ booking.contactPhone || '-' }}</td>
            <td class="print-td">{{ booking.guestCount ?? '-' }}</td>
            <td class="print-td print-table-cell"></td>
            <td class="print-td">
              <span v-if="booking.hasComment" class="print-comment">✓</span>
              <span v-else>-</span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
    <div v-else class="print-empty-section">
      Engar borðapantanir fundust
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { useTableBookings } from '~/composables/useTableBookings';
import { useBookings } from '~/composables/useBookings';
import type { BookingDto } from '~/types/booking';

// Disable default layout for print page
definePageMeta({
  layout: false
});

const { getTableBookings } = useTableBookings();
const { getWeekBookings } = useBookings();

const route = useRoute();
const selectedDate = ref<string>(route.query.date as string || new Date().toISOString().split('T')[0]);
const bookingsData = ref<any>(null);
const otherBookingsData = ref<BookingDto[]>([]);
const loading = ref(true);

// Filter and sort bookings - only "Ný" status, sorted by time
const filteredBookings = computed(() => {
  if (!bookingsData.value?.bookings) return [];
  
  return bookingsData.value.bookings
    .filter((booking: any) => booking.status === 'Ný')
    .sort((a: any, b: any) => {
      if (!a.timestamp || !b.timestamp) return 0;
      const timeA = new Date(a.timestamp).getTime();
      const timeB = new Date(b.timestamp).getTime();
      return timeA - timeB;
    });
});

const loadData = async () => {
  loading.value = true;
  
  try {
    const fromDate = new Date(selectedDate.value);
    const toDate = new Date(selectedDate.value);
    toDate.setDate(toDate.getDate() + 1);
    
    // Load both in parallel
    const [tableBookingsResult, weekBookingsResult] = await Promise.allSettled([
      getTableBookings(
        fromDate,
        toDate,
        undefined,
        undefined,
        undefined,
        1,
        1000 // Large page size to get all bookings
      ),
      (async () => {
        const date = new Date(selectedDate.value);
        date.setHours(0, 0, 0, 0);
        const unixTimestamp = Math.floor(date.getTime() / 1000);
        const weekData = await getWeekBookings(unixTimestamp);
        const selectedDateStr = selectedDate.value;
        const dayBookings = weekData.days?.find((day: any) => {
          const dayDate = new Date(day.date);
          const dayDateStr = dayDate.toISOString().split('T')[0];
          return dayDateStr === selectedDateStr;
        });
        return dayBookings?.bookings || [];
      })()
    ]);
    
    if (tableBookingsResult.status === 'fulfilled') {
      bookingsData.value = tableBookingsResult.value;
    }
    
    if (weekBookingsResult.status === 'fulfilled') {
      otherBookingsData.value = weekBookingsResult.value;
    }
  } catch (err) {
    console.error('Error loading print data:', err);
  } finally {
    loading.value = false;
    // Trigger print dialog after a short delay to ensure content is rendered
    setTimeout(() => {
      window.print();
    }, 500);
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

const formatTime = (dateString: string): string => {
  try {
    const date = new Date(dateString);
    const hours = date.getHours().toString().padStart(2, '0');
    const minutes = date.getMinutes().toString().padStart(2, '0');
    return `${hours}:${minutes}`;
  } catch {
    return dateString;
  }
};

onMounted(() => {
  loadData();
});
</script>

<style scoped>
/* Print-specific styles */
@media print {
  @page {
    size: A4;
    margin: 0.5cm;
  }

  body {
    margin: 0;
    padding: 0;
  }

  .print-container {
    width: 100%;
    padding: 0;
  }

  .print-header {
    margin-bottom: 0.5cm;
  }

  .print-title {
    font-size: 18pt;
    font-weight: bold;
    margin: 0;
  }

  .print-section {
    margin-bottom: 0.5cm;
    page-break-inside: avoid;
  }

  .print-section-header {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-bottom: 4px;
    border-bottom: 1px solid #000;
    padding-bottom: 2px;
  }

  .print-section-title {
    font-size: 14pt;
    font-weight: bold;
    margin: 0;
  }

  .print-badge {
    font-size: 10pt;
    padding: 2px 6px;
    background: #e5e7eb;
    border-radius: 4px;
  }

  .print-other-bookings {
    font-size: 10pt;
  }

  .print-other-booking-item {
    display: flex;
    gap: 8px;
    padding: 2px 0;
    border-bottom: 1px dotted #ccc;
  }

  .print-other-booking-item:last-child {
    border-bottom: none;
  }

  .print-time {
    font-weight: bold;
    min-width: 60px;
  }

  .print-name {
    font-weight: 500;
    min-width: 120px;
  }

  .print-description {
    font-style: italic;
    color: #666;
    flex: 1;
  }

  .print-guests {
    font-size: 9pt;
    color: #666;
  }

  .print-table-container {
    margin-top: 0.5cm;
  }

  .print-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 10pt;
  }

  .print-th {
    border: 1px solid #000;
    padding: 4px 6px;
    background: #f3f4f6;
    font-weight: bold;
    text-align: left;
    font-size: 9pt;
  }

  .print-td {
    border: 1px solid #000;
    padding: 4px 6px;
    text-align: left;
  }

  .print-tr {
    page-break-inside: avoid;
  }

  .print-table-cell {
    text-align: center;
  }

  .print-comment {
    font-weight: bold;
    color: #000;
  }

  .print-empty,
  .print-empty-section,
  .print-empty-cell {
    color: #999;
  }
}

/* Screen styles (for preview) */
@media screen {
  .print-container {
    max-width: 210mm;
    margin: 20px auto;
    padding: 20px;
    background: white;
    box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);
  }

  .print-header {
    margin-bottom: 20px;
  }

  .print-title {
    font-size: 24px;
    font-weight: bold;
  }

  .print-section {
    margin-bottom: 20px;
    padding: 10px;
    border: 1px solid #e5e7eb;
    border-radius: 4px;
  }

  .print-section-header {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-bottom: 8px;
    padding-bottom: 8px;
    border-bottom: 1px solid #e5e7eb;
  }

  .print-section-title {
    font-size: 16px;
    font-weight: bold;
  }

  .print-badge {
    font-size: 12px;
    padding: 2px 8px;
    background: #e5e7eb;
    border-radius: 12px;
  }

  .print-other-bookings {
    font-size: 12px;
  }

  .print-other-booking-item {
    display: flex;
    gap: 12px;
    padding: 4px 0;
    border-bottom: 1px dotted #ccc;
  }

  .print-other-booking-item:last-child {
    border-bottom: none;
  }

  .print-time {
    font-weight: bold;
    min-width: 80px;
  }

  .print-name {
    font-weight: 500;
    min-width: 150px;
  }

  .print-description {
    font-style: italic;
    color: #666;
    flex: 1;
  }

  .print-guests {
    font-size: 11px;
    color: #666;
  }

  .print-table-container {
    margin-top: 20px;
  }

  .print-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 12px;
  }

  .print-th {
    border: 1px solid #d1d5db;
    padding: 8px;
    background: #f9fafb;
    font-weight: bold;
    text-align: left;
  }

  .print-td {
    border: 1px solid #d1d5db;
    padding: 8px;
    text-align: left;
  }

  .print-table-cell {
    text-align: center;
  }

  .print-comment {
    font-weight: bold;
    color: #4f46e5;
  }

  .print-empty,
  .print-empty-section,
  .print-empty-cell {
    color: #9ca3af;
  }
}
</style>

