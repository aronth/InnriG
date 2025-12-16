<template>
  <div class="min-h-[80vh]">
    <WaitTimeSubNav />
    <div class="mb-8">
      <h1 class="text-3xl font-bold text-gray-800 mb-2">Stillingar biðtíma</h1>
      <p class="text-gray-600">Stilla Pushover tilkynningar fyrir biðtíma</p>
    </div>

    <div class="bg-white rounded-lg shadow p-6 mb-6">
      <h2 class="text-xl font-bold text-gray-800 mb-4">Bæta við tilkynningu</h2>
      <WaitTimeNotificationForm @saved="loadNotifications" />
    </div>

    <div class="bg-white rounded-lg shadow p-6">
      <h2 class="text-xl font-bold text-gray-800 mb-4">Núverandi tilkynningar</h2>

      <div v-if="loading" class="text-center py-8">
        <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600 mx-auto"></div>
      </div>

      <div v-else-if="notifications.length === 0" class="text-center py-8 text-gray-500">
        <p>Engar tilkynningar skilgreindar</p>
      </div>

      <div v-else class="space-y-4">
        <div
          v-for="notification in notifications"
          :key="notification.id"
          class="border rounded-lg p-4"
        >
          <div class="flex justify-between items-start">
            <div class="flex-1">
              <h3 class="font-semibold text-gray-800 mb-2">{{ notification.restaurantName }}</h3>
              <div class="space-y-2 text-sm text-gray-600">
                <p v-if="notification.sottThresholdMinutes">
                  <strong>Sótt þröskuldur:</strong> {{ notification.sottThresholdMinutes }} mínútur
                </p>
                <p v-if="notification.sentThresholdMinutes">
                  <strong>Sent/Heimsent þröskuldur:</strong> {{ notification.sentThresholdMinutes }} mínútur
                </p>
                <p>
                  <strong>Staða:</strong>
                  <span
                    :class="[
                      'ml-2 px-2 py-1 rounded text-xs',
                      notification.isEnabled
                        ? 'bg-green-100 text-green-800'
                        : 'bg-gray-100 text-gray-800'
                    ]"
                  >
                    {{ notification.isEnabled ? 'Virk' : 'Óvirk' }}
                  </span>
                </p>
              </div>
            </div>
            <div class="flex gap-2">
              <button
                @click="editNotification(notification)"
                class="px-3 py-1 bg-indigo-600 text-white rounded hover:bg-indigo-700 transition-colors text-sm"
              >
                Breyta
              </button>
              <button
                @click="deleteNotification(notification.id)"
                class="px-3 py-1 bg-red-600 text-white rounded hover:bg-red-700 transition-colors text-sm"
              >
                Eyða
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Edit Modal -->
    <div
      v-if="editingNotification"
      class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50"
      @click.self="editingNotification = null"
    >
      <div class="bg-white rounded-lg p-6 max-w-md w-full mx-4">
        <h3 class="text-xl font-bold text-gray-800 mb-4">Breyta tilkynningu</h3>
        <WaitTimeNotificationForm
          :notification="editingNotification"
          @saved="handleSaved"
          @cancelled="editingNotification = null"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
const { getNotifications, deleteNotification: deleteNotificationApi, updateNotification } = useWaitTimeNotifications()

const notifications = ref<any[]>([])
const loading = ref(true)
const editingNotification = ref<any | null>(null)

const loadNotifications = async () => {
  try {
    loading.value = true
    notifications.value = await getNotifications()
  } catch (err: any) {
    console.error('Error loading notifications:', err)
  } finally {
    loading.value = false
  }
}

const editNotification = (notification: any) => {
  editingNotification.value = notification
}

const deleteNotification = async (id: string) => {
  if (!confirm('Ertu viss um að þú viljir eyða þessari tilkynningu?')) {
    return
  }

  try {
    await deleteNotificationApi(id)
    await loadNotifications()
  } catch (err: any) {
    alert('Villa kom upp við að eyða tilkynningu: ' + (err.message || 'Óþekkt villa'))
  }
}

const handleSaved = () => {
  editingNotification.value = null
  loadNotifications()
}

onMounted(() => {
  loadNotifications()
})
</script>
