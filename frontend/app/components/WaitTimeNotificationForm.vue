<template>
  <form @submit.prevent="handleSubmit" class="space-y-4">
    <div>
      <label class="block text-sm font-medium text-gray-700 mb-2">
        Veitingastaður
      </label>
      <select
        v-model="form.restaurant"
        :disabled="!!notification"
        required
        class="w-full border rounded-lg px-3 py-2"
      >
        <option :value="0">Greifinn</option>
        <option :value="1">Spretturinn</option>
      </select>
    </div>

    <div>
      <label class="block text-sm font-medium text-gray-700 mb-2">
        Pushover User Key
      </label>
      <input
        v-model="form.pushoverUserKey"
        type="text"
        required
        placeholder="Your Pushover user key"
        class="w-full border rounded-lg px-3 py-2"
      />
      <p class="text-xs text-gray-500 mt-1">
        Finndu þetta á <a href="https://pushover.net/" target="_blank" class="text-indigo-600 hover:underline">pushover.net</a>
      </p>
    </div>

    <div>
      <label class="block text-sm font-medium text-gray-700 mb-2">
        Sótt þröskuldur (mínútur)
      </label>
      <input
        v-model.number="form.sottThresholdMinutes"
        type="number"
        min="0"
        placeholder="Til dæmis: 30"
        class="w-full border rounded-lg px-3 py-2"
      />
      <p class="text-xs text-gray-500 mt-1">
        Tilkynning verður send þegar biðtími fer yfir þessa tölu (valfrjálst)
      </p>
    </div>

    <div>
      <label class="block text-sm font-medium text-gray-700 mb-2">
        Sent/Heimsent þröskuldur (mínútur)
      </label>
      <input
        v-model.number="form.sentThresholdMinutes"
        type="number"
        min="0"
        placeholder="Til dæmis: 45"
        class="w-full border rounded-lg px-3 py-2"
      />
      <p class="text-xs text-gray-500 mt-1">
        Tilkynning verður send þegar biðtími fer yfir þessa tölu (valfrjálst)
      </p>
    </div>

    <div class="flex items-center">
      <input
        v-model="form.isEnabled"
        type="checkbox"
        id="isEnabled"
        class="h-4 w-4 text-indigo-600 focus:ring-indigo-500 border-gray-300 rounded"
      />
      <label for="isEnabled" class="ml-2 block text-sm text-gray-700">
        Virkja tilkynningu
      </label>
    </div>

    <div v-if="error" class="bg-red-50 border border-red-200 rounded-lg p-3">
      <p class="text-sm text-red-800">{{ error }}</p>
    </div>

    <div class="flex gap-3">
      <button
        type="submit"
        :disabled="submitting"
        class="flex-1 px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
      >
        <span v-if="submitting">Vista...</span>
        <span v-else>{{ notification ? 'Uppfæra' : 'Vista' }}</span>
      </button>
      <button
        v-if="notification"
        type="button"
        @click="$emit('cancelled')"
        class="px-4 py-2 bg-gray-200 text-gray-700 rounded-lg hover:bg-gray-300 transition-colors"
      >
        Hætta við
      </button>
    </div>
  </form>
</template>

<script setup lang="ts">
interface Props {
  notification?: any
}

enum Restaurant {
  Greifinn = 0,
  Spretturinn = 1
}

const props = defineProps<Props>()

const emit = defineEmits<{
  saved: []
  cancelled: []
}>()

const { createNotification, updateNotification } = useWaitTimeNotifications()

const form = ref({
  restaurant: props.notification?.restaurant ?? 0 as Restaurant,
  pushoverUserKey: props.notification?.pushoverUserKey ?? '',
  sottThresholdMinutes: props.notification?.sottThresholdMinutes ?? undefined,
  sentThresholdMinutes: props.notification?.sentThresholdMinutes ?? undefined,
  isEnabled: props.notification?.isEnabled ?? true
})

const submitting = ref(false)
const error = ref<string | null>(null)

const handleSubmit = async () => {
  try {
    submitting.value = true
    error.value = null

    if (props.notification) {
      // Update existing
      await updateNotification(props.notification.id, {
        pushoverUserKey: form.value.pushoverUserKey,
        sottThresholdMinutes: form.value.sottThresholdMinutes,
        sentThresholdMinutes: form.value.sentThresholdMinutes,
        isEnabled: form.value.isEnabled
      })
    } else {
      // Create new
      await createNotification({
        restaurant: form.value.restaurant,
        pushoverUserKey: form.value.pushoverUserKey,
        sottThresholdMinutes: form.value.sottThresholdMinutes,
        sentThresholdMinutes: form.value.sentThresholdMinutes,
        isEnabled: form.value.isEnabled
      })
    }

    emit('saved')
    
    // Reset form if creating new
    if (!props.notification) {
      form.value = {
        restaurant: 0,
        pushoverUserKey: '',
        sottThresholdMinutes: undefined,
        sentThresholdMinutes: undefined,
        isEnabled: true
      }
    }
  } catch (err: any) {
    error.value = err.data?.message || err.message || 'Villa kom upp við að vista'
  } finally {
    submitting.value = false
  }
}
</script>
