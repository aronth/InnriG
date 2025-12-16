<template>
  <div class="min-h-[80vh]">
    <WaitTimeSubNav />
    <div class="mb-8">
      <h1 class="text-3xl font-bold text-gray-800 mb-2">Biðtímar</h1>
      <p class="text-gray-600">Fylgstu með biðtímum hjá Greifinn og Spretturinn</p>
    </div>

    <div v-if="loading" class="flex justify-center items-center py-12">
      <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600"></div>
    </div>

    <div v-else-if="error" class="bg-red-50 border border-red-200 rounded-lg p-4 mb-6">
      <p class="text-red-800">{{ error }}</p>
    </div>

    <div v-else class="grid md:grid-cols-2 gap-6 mb-8">
      <WaitTimeCard
        v-for="record in latestRecords"
        :key="record.id"
        :record="record"
      />
    </div>

    <div class="bg-white rounded-lg shadow p-6">
      <div class="flex justify-between items-center mb-4">
        <h2 class="text-xl font-bold text-gray-800">Tilkynningar</h2>
        <NuxtLink
          to="/wait-times/settings"
          class="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors"
        >
          Stillingar
        </NuxtLink>
      </div>

      <div v-if="notificationsLoading" class="text-center py-4">
        <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600 mx-auto"></div>
      </div>

      <div v-else-if="notifications.length === 0" class="text-center py-8 text-gray-500">
        <p>Engar tilkynningar skilgreindar</p>
        <NuxtLink
          to="/wait-times/settings"
          class="text-indigo-600 hover:text-indigo-700 underline mt-2 inline-block"
        >
          Bæta við tilkynningum
        </NuxtLink>
      </div>

      <div v-else class="space-y-4">
        <div
          v-for="notification in notifications"
          :key="notification.id"
          class="border rounded-lg p-4"
        >
          <div class="flex justify-between items-start">
            <div>
              <h3 class="font-semibold text-gray-800">{{ notification.restaurantName }}</h3>
              <div class="mt-2 space-y-1 text-sm text-gray-600">
                <p v-if="notification.sottThresholdMinutes">
                  Sótt þröskuldur: {{ notification.sottThresholdMinutes }} mínútur
                </p>
                <p v-if="notification.sentThresholdMinutes">
                  Sent/Heimsent þröskuldur: {{ notification.sentThresholdMinutes }} mínútur
                </p>
              </div>
            </div>
            <div class="flex items-center gap-2">
              <span
                :class="[
                  'px-3 py-1 rounded-full text-xs font-medium',
                  notification.isEnabled
                    ? 'bg-green-100 text-green-800'
                    : 'bg-gray-100 text-gray-800'
                ]"
              >
                {{ notification.isEnabled ? 'Virk' : 'Óvirk' }}
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
const { getLatestRecords } = useWaitTimeRecords()
const { getNotifications } = useWaitTimeNotifications()

const latestRecords = ref<any[]>([])
const notifications = ref<any[]>([])
const loading = ref(true)
const notificationsLoading = ref(true)
const error = ref<string | null>(null)

const loadData = async () => {
  try {
    loading.value = true
    error.value = null
    latestRecords.value = await getLatestRecords()
  } catch (err: any) {
    error.value = err.message || 'Villa kom upp við að sækja biðtíma'
  } finally {
    loading.value = false
  }
}

const loadNotifications = async () => {
  try {
    notificationsLoading.value = true
    notifications.value = await getNotifications()
  } catch (err: any) {
    console.error('Error loading notifications:', err)
  } finally {
    notificationsLoading.value = false
  }
}

onMounted(() => {
  loadData()
  loadNotifications()
  
  // Refresh every 30 seconds
  const interval = setInterval(() => {
    loadData()
  }, 30000)
  
  onUnmounted(() => {
    clearInterval(interval)
  })
})
</script>
