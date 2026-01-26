<template>
  <div class="space-y-8">
    <!-- Bookings Week Widget -->
    <BookingsWeekWidget />

    <!-- Bókanir Section -->
    <div>
      <h2 class="text-xl font-semibold text-gray-900 mb-4">Bókanir</h2>
      <div class="flex flex-wrap gap-4">
        <!-- Borðapantanir -->
        <div class="group relative bg-white rounded-xl p-6 shadow-sm border border-gray-200 hover:shadow-lg hover:border-indigo-300 transition-all duration-200 min-w-[500px] flex-1">
          <div class="flex items-center gap-4 mb-4">
            <div class="w-12 h-12 bg-gradient-to-br from-emerald-500 to-emerald-600 rounded-lg flex items-center justify-center shadow-md flex-shrink-0">
              <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
              </svg>
            </div>
            <div class="flex-1 min-w-0">
              <h3 class="text-lg font-semibold text-gray-900 mb-1">Borðapantanir</h3>
              <p class="text-sm text-gray-600">Skoða borðapantanir á Greifinn</p>
            </div>
            <a
              :href="bookingUrl"
              target="_blank"
              rel="noopener noreferrer"
              class="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors font-medium flex-shrink-0"
            >
              Go
            </a>
          </div>
          
          <div class="flex items-center gap-2">
            <input
              id="booking-date"
              v-model="bookingDate"
              type="date"
              class="flex-1 px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            />
            <button
              @click="bookingDate = todayFormatted"
              class="px-4 py-2 text-sm bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200 transition-colors font-medium"
            >
              Today
            </button>
          </div>
        </div>

        <!-- Gamli BókAri -->
        <div class="group relative bg-white rounded-xl p-6 shadow-sm border border-gray-200 hover:shadow-lg hover:border-indigo-300 transition-all duration-200 min-w-[500px] flex-1">
          <div class="flex items-center gap-4 mb-4">
            <div class="w-12 h-12 bg-gradient-to-br from-teal-500 to-teal-600 rounded-lg flex items-center justify-center shadow-md flex-shrink-0">
              <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
            </div>
            <div class="flex-1 min-w-0">
              <h3 class="text-lg font-semibold text-gray-900 mb-1">Gamli BókAri</h3>
              <p class="text-sm text-gray-600">Skoða pantanir í gamla BókAra</p>
            </div>
            <a
              :href="bokariUrl"
              target="_blank"
              rel="noopener noreferrer"
              class="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors font-medium flex-shrink-0"
            >
              Go
            </a>
          </div>
          
          <div class="flex items-center gap-2">
            <input
              id="bokari-date"
              v-model="bokariDate"
              type="date"
              class="flex-1 px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
            />
            <button
              @click="bokariDate = todayFormatted"
              class="px-4 py-2 text-sm bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200 transition-colors font-medium"
            >
              Today
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Greining Section -->
    <div>
      <h2 class="text-xl font-semibold text-gray-900 mb-4">Greining</h2>
      <div class="flex flex-wrap gap-4">
        <!-- Dashboard / Stjórnborð -->
        <NuxtLink
          to="/products"
          class="group relative bg-white rounded-xl p-6 shadow-sm border border-gray-200 hover:shadow-lg hover:border-indigo-300 transition-all duration-200 min-w-[500px] flex-1"
        >
          <div class="flex items-center gap-4">
            <div class="w-12 h-12 bg-gradient-to-br from-indigo-600 to-indigo-700 rounded-lg flex items-center justify-center shadow-md flex-shrink-0">
              <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
              </svg>
            </div>
            <div class="flex-1 min-w-0">
              <h3 class="text-lg font-semibold text-gray-900 mb-1">Stjórnborð</h3>
              <p class="text-sm text-gray-600">Yfirlit yfir KPIs og tölfræði</p>
            </div>
            <span class="px-4 py-2 bg-indigo-600 text-white rounded-lg font-medium flex-shrink-0">Go</span>
          </div>
        </NuxtLink>

        <!-- Products / Verðgreining -->
        <NuxtLink
          to="/products"
          class="group relative bg-white rounded-xl p-6 shadow-sm border border-gray-200 hover:shadow-lg hover:border-indigo-300 transition-all duration-200 min-w-[500px] flex-1"
        >
          <div class="flex items-center gap-4">
            <div class="w-12 h-12 bg-gradient-to-br from-indigo-500 to-indigo-600 rounded-lg flex items-center justify-center shadow-md flex-shrink-0">
              <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
              </svg>
            </div>
            <div class="flex-1 min-w-0">
              <h3 class="text-lg font-semibold text-gray-900 mb-1">Verðgreining</h3>
              <p class="text-sm text-gray-600">Skoða vörur, verðsamanburð og verðsögu</p>
            </div>
            <span class="px-4 py-2 bg-indigo-600 text-white rounded-lg font-medium flex-shrink-0">Go</span>
          </div>
        </NuxtLink>

        <!-- Invoices / Reikningar -->
        <NuxtLink
          to="/products/invoices"
          class="group relative bg-white rounded-xl p-6 shadow-sm border border-gray-200 hover:shadow-lg hover:border-indigo-300 transition-all duration-200 min-w-[500px] flex-1"
        >
          <div class="flex items-center gap-4">
            <div class="w-12 h-12 bg-gradient-to-br from-purple-500 to-purple-600 rounded-lg flex items-center justify-center shadow-md flex-shrink-0">
              <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
            </div>
            <div class="flex-1 min-w-0">
              <h3 class="text-lg font-semibold text-gray-900 mb-1">Reikningar</h3>
              <p class="text-sm text-gray-600">Hlaða upp og stjórna reikningum</p>
            </div>
            <span class="px-4 py-2 bg-indigo-600 text-white rounded-lg font-medium flex-shrink-0">Go</span>
          </div>
        </NuxtLink>

        <!-- Orders / Pantanir -->
        <NuxtLink
          to="/orders"
          class="group relative bg-white rounded-xl p-6 shadow-sm border border-gray-200 hover:shadow-lg hover:border-indigo-300 transition-all duration-200 min-w-[500px] flex-1"
        >
          <div class="flex items-center gap-4">
            <div class="w-12 h-12 bg-gradient-to-br from-pink-500 to-pink-600 rounded-lg flex items-center justify-center shadow-md flex-shrink-0">
              <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z" />
              </svg>
            </div>
            <div class="flex-1 min-w-0">
              <h3 class="text-lg font-semibold text-gray-900 mb-1">Pantanir</h3>
              <p class="text-sm text-gray-600">Stjórna pantanir og skoða skýrslur</p>
            </div>
            <span class="px-4 py-2 bg-indigo-600 text-white rounded-lg font-medium flex-shrink-0">Go</span>
          </div>
        </NuxtLink>
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

// Booking date functionality
const today = new Date()
const todayFormatted = today.toISOString().split('T')[0]
const bookingDate = ref(todayFormatted)
const bokariDate = ref(todayFormatted)

// Format date to Icelandic format (dd.MM.yyyy)
const formatIcelandicDate = (date: Date): string => {
  const day = String(date.getDate()).padStart(2, '0')
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const year = date.getFullYear()
  return `${day}.${month}.${year}`
}

// Convert date to Unix timestamp (seconds since epoch)
const dateToUnixTimestamp = (date: Date): number => {
  return Math.floor(date.getTime() / 1000)
}

// Build booking URL
const bookingUrl = computed(() => {
  const selectedDate = new Date(bookingDate.value)
  const nextDay = new Date(selectedDate)
  nextDay.setDate(nextDay.getDate() + 1)
  
  const fromDate = formatIcelandicDate(selectedDate)
  const toDate = formatIcelandicDate(nextDay)
  
  const baseUrl = 'https://www.greifinn.is/is/bordapontun/booking/index'
  const params = new URLSearchParams({
    'flt-timestamp[from]': fromDate,
    'flt-timestamp[to]': toDate
  })
  
  return `${baseUrl}?${params.toString()}`
})

// Build Gamli BókAri URL
const bokariUrl = computed(() => {
  const selectedDate = new Date(bokariDate.value)
  // Set to start of day to get consistent timestamp
  selectedDate.setHours(0, 0, 0, 0)
  const timestamp = dateToUnixTimestamp(selectedDate)
  
  return `http://greifi.stefna.is/default/journal/?dt=${timestamp}`
})
</script>
