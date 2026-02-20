<template>
  <div class="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50" @click.self="handleClose">
    <div class="bg-white rounded-lg shadow-xl w-full max-w-md m-4">
      <div class="px-6 py-4 border-b border-gray-200 flex items-center justify-between">
        <h3 class="text-lg font-bold text-gray-900">
          Tengja netfang
        </h3>
        <button
          @click="handleClose"
          class="text-gray-400 hover:text-gray-600 focus:outline-none"
          :disabled="polling"
        >
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>

      <div class="px-6 py-4">
        <!-- Email Input Form (shown when emailAddress is not provided) -->
        <div v-if="!emailAddressProvided && !loading && !success && !error">
          <div class="mb-4">
            <label class="block text-sm font-medium text-gray-700 mb-2">
              Netfang *
            </label>
            <input
              v-model="inputEmailAddress"
              type="email"
              class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
              placeholder="nafn@example.com"
              @keyup.enter="startConnection"
            />
          </div>

          <div class="mb-4">
            <label class="flex items-center">
              <input
                v-model="inputIsSystemInbox"
                type="checkbox"
                class="rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
              />
              <span class="ml-2 text-sm text-gray-700">Kerfisinnhólf</span>
            </label>
            <p class="mt-1 text-xs text-gray-500">
              Merktu þetta ef þetta er kerfisinnhólf sem verður notað til að lesa tölvupósta
            </p>
          </div>
        </div>

        <!-- Loading State -->
        <div v-if="loading" class="text-center py-8">
          <div class="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600"></div>
          <p class="mt-2 text-sm text-gray-600">Byrja tengingu...</p>
        </div>

        <!-- Device Code Display -->
        <div v-else-if="connectionInfo && !success && !error">
          <div class="mb-4">
            <p class="text-sm text-gray-700 mb-2">
              Fylgdu þessum skrefum til að tengja netfangið <strong>{{ props.emailAddress || inputEmailAddress }}</strong>:
            </p>
            <ol class="list-decimal list-inside text-sm text-gray-600 space-y-1 mb-4">
              <li>Opnaðu eftirfarandi slóð í vafranum þínum</li>
              <li>Sláðu inn kóðann sem birtist hér fyrir neðan</li>
              <li>Skráðu þig inn með Microsoft reikningnum þínum</li>
            </ol>
          </div>

          <div class="mb-4">
            <label class="block text-sm font-medium text-gray-700 mb-2">
              Slóð
            </label>
            <div class="flex items-center gap-2">
              <input
                :value="connectionInfo.verificationUrl"
                readonly
                class="flex-1 px-3 py-2 border border-gray-300 rounded-md bg-gray-50 text-sm"
              />
              <button
                @click="copyToClipboard(connectionInfo.verificationUrl)"
                class="px-3 py-2 text-sm bg-gray-100 text-gray-700 rounded-md hover:bg-gray-200 focus:outline-none focus:ring-2 focus:ring-gray-500"
                title="Afrita slóð"
              >
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z" />
                </svg>
              </button>
            </div>
          </div>

          <div class="mb-4">
            <label class="block text-sm font-medium text-gray-700 mb-2">
              Kóði
            </label>
            <div class="flex items-center gap-2">
              <div class="flex-1 px-4 py-3 bg-gray-50 border-2 border-dashed border-gray-300 rounded-md">
                <p class="text-2xl font-mono font-bold text-center text-gray-900 tracking-wider">
                  {{ connectionInfo.deviceCode }}
                </p>
              </div>
              <button
                @click="copyToClipboard(connectionInfo.deviceCode)"
                class="px-3 py-2 text-sm bg-gray-100 text-gray-700 rounded-md hover:bg-gray-200 focus:outline-none focus:ring-2 focus:ring-gray-500"
                title="Afrita kóða"
              >
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z" />
                </svg>
              </button>
            </div>
          </div>

          <!-- Countdown Timer -->
          <div class="mb-4">
            <div class="flex items-center justify-between text-sm text-gray-600 mb-1">
              <span>Kóði rennur út eftir:</span>
              <span class="font-semibold">{{ formatTimeRemaining(timeRemaining) }}</span>
            </div>
            <div class="w-full bg-gray-200 rounded-full h-2">
              <div
                class="bg-indigo-600 h-2 rounded-full transition-all duration-1000"
                :style="{ width: `${(timeRemaining / connectionInfo.expiresIn) * 100}%` }"
              ></div>
            </div>
          </div>

          <!-- Polling Status -->
          <div v-if="polling" class="mb-4 p-3 bg-blue-50 border border-blue-200 rounded-md">
            <div class="flex items-center gap-2">
              <div class="inline-block animate-spin rounded-full h-4 w-4 border-b-2 border-blue-600"></div>
              <p class="text-sm text-blue-800">Bíður eftir auðkenningu...</p>
            </div>
          </div>

          <!-- Error Message -->
          <div v-if="pollError" class="mb-4 p-3 bg-red-50 border border-red-200 rounded-md">
            <p class="text-sm text-red-800">{{ pollError }}</p>
          </div>
        </div>

        <!-- Success State -->
        <div v-else-if="success" class="text-center py-8">
          <div class="inline-flex items-center justify-center w-16 h-16 rounded-full bg-green-100 mb-4">
            <svg class="w-8 h-8 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
            </svg>
          </div>
          <h4 class="text-lg font-semibold text-gray-900 mb-2">Tenging tókst!</h4>
          <p class="text-sm text-gray-600 mb-4">
            Netfangið <strong>{{ connectedEmail }}</strong> hefur verið tengt.
          </p>
        </div>

        <!-- Error State -->
        <div v-else-if="error" class="text-center py-8">
          <div class="inline-flex items-center justify-center w-16 h-16 rounded-full bg-red-100 mb-4">
            <svg class="w-8 h-8 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </div>
          <h4 class="text-lg font-semibold text-gray-900 mb-2">Villa kom upp</h4>
          <p class="text-sm text-gray-600 mb-4">{{ error }}</p>
        </div>
      </div>

      <div class="px-6 py-4 border-t border-gray-200 flex items-center justify-end gap-3">
        <button
          v-if="!emailAddressProvided && !loading && !success && !error"
          @click="handleClose"
          class="px-4 py-2 text-gray-700 bg-gray-100 rounded-md hover:bg-gray-200 focus:outline-none focus:ring-2 focus:ring-gray-500"
        >
          Hætta við
        </button>
        <button
          v-if="!emailAddressProvided && !loading && !success && !error"
          @click="startConnection"
          :disabled="!inputEmailAddress.trim()"
          class="px-4 py-2 text-white bg-indigo-600 rounded-md hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          Byrja tengingu
        </button>
        <button
          v-if="emailAddressProvided && !success && !loading"
          @click="handleClose"
          class="px-4 py-2 text-gray-700 bg-gray-100 rounded-md hover:bg-gray-200 focus:outline-none focus:ring-2 focus:ring-gray-500"
          :disabled="polling"
        >
          Hætta við
        </button>
        <button
          v-if="success"
          @click="handleSuccess"
          class="px-4 py-2 text-white bg-indigo-600 rounded-md hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500"
        >
          Lokið
        </button>
        <button
          v-if="error && !loading"
          @click="retry"
          class="px-4 py-2 text-white bg-indigo-600 rounded-md hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500"
        >
          Reyna aftur
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { EmailConnectionDto } from '~/types/email'

interface Props {
  emailAddress?: string
  isSystemInbox?: boolean
}

interface Emits {
  (e: 'close'): void
  (e: 'success', emailAddress: string): void
}

const props = withDefaults(defineProps<Props>(), {
  emailAddress: '',
  isSystemInbox: false
})
const emit = defineEmits<Emits>()

const { connectEmail, pollForToken } = useEmailAuth()

const emailAddressProvided = computed(() => !!props.emailAddress)
const inputEmailAddress = ref(props.emailAddress || '')
const inputIsSystemInbox = ref(props.isSystemInbox || false)

const loading = ref(false)
const connectionInfo = ref<EmailConnectionDto | null>(null)
const polling = ref(false)
const success = ref(false)
const error = ref<string | null>(null)
const pollError = ref<string | null>(null)
const connectedEmail = ref<string>('')
const timeRemaining = ref(0)
const countdownInterval = ref<NodeJS.Timeout | null>(null)
const pollInterval = ref<NodeJS.Timeout | null>(null)

const formatTimeRemaining = (seconds: number): string => {
  const mins = Math.floor(seconds / 60)
  const secs = seconds % 60
  return `${mins}:${secs.toString().padStart(2, '0')}`
}

const copyToClipboard = async (text: string) => {
  try {
    await navigator.clipboard.writeText(text)
    // Could show a toast notification here
  } catch (err) {
    console.error('Failed to copy to clipboard:', err)
  }
}

const startCountdown = (expiresIn: number) => {
  timeRemaining.value = expiresIn
  if (countdownInterval.value) {
    clearInterval(countdownInterval.value)
  }
  countdownInterval.value = setInterval(() => {
    timeRemaining.value--
    if (timeRemaining.value <= 0) {
      if (countdownInterval.value) {
        clearInterval(countdownInterval.value)
        countdownInterval.value = null
      }
      if (!success.value && !error.value) {
        error.value = 'Kóði rann út. Vinsamlegast reyndu aftur.'
        stopPolling()
      }
    }
  }, 1000)
}

const startPolling = async (deviceCode: string, interval: number) => {
  polling.value = true
  pollError.value = null

  const poll = async () => {
    try {
      const result = await pollForToken(deviceCode)
      if (result.success) {
        success.value = true
        connectedEmail.value = result.emailAddress || props.emailAddress
        stopPolling()
        if (countdownInterval.value) {
          clearInterval(countdownInterval.value)
          countdownInterval.value = null
        }
        // Wait a moment before calling success callback
        setTimeout(() => {
          emit('success', connectedEmail.value)
        }, 1500)
      }
    } catch (err: any) {
      const errorMessage = err.data?.error || err.message || 'Villa kom upp við að tengja netfang'
      if (errorMessage.includes('expired') || errorMessage.includes('út')) {
        error.value = 'Kóði rann út. Vinsamlegast reyndu aftur.'
        stopPolling()
      } else if (errorMessage.includes('authorization_pending') || errorMessage.includes('pending')) {
        // Still waiting, continue polling
        pollError.value = null
      } else {
        pollError.value = errorMessage
      }
    }
  }

  // Poll immediately, then at intervals
  await poll()
  if (!success.value && !error.value) {
    pollInterval.value = setInterval(poll, interval * 1000)
  }
}

const stopPolling = () => {
  polling.value = false
  if (pollInterval.value) {
    clearInterval(pollInterval.value)
    pollInterval.value = null
  }
}

const handleClose = () => {
  if (!polling.value) {
    stopPolling()
    if (countdownInterval.value) {
      clearInterval(countdownInterval.value)
      countdownInterval.value = null
    }
    emit('close')
  }
}

const handleSuccess = () => {
  emit('success', connectedEmail.value)
}

const retry = () => {
  error.value = null
  pollError.value = null
  success.value = false
  const email = props.emailAddress || inputEmailAddress.value
  const isSystem = props.isSystemInbox || inputIsSystemInbox.value
  if (email) {
    loading.value = true
    initialize(email, isSystem)
  } else {
    loading.value = false
  }
}

const startConnection = async () => {
  if (!inputEmailAddress.value.trim()) {
    error.value = 'Vinsamlegast sláðu inn netfang'
    return
  }

  await initialize(inputEmailAddress.value, inputIsSystemInbox.value)
}

const initialize = async (emailAddress: string, isSystemInbox: boolean) => {
  try {
    loading.value = true
    error.value = null
    const connection = await connectEmail(emailAddress, isSystemInbox)
    connectionInfo.value = connection
    loading.value = false
    startCountdown(connection.expiresIn)
    await startPolling(connection.deviceCode, connection.interval)
  } catch (err: any) {
    loading.value = false
    error.value = err.data?.error || err.message || 'Villa kom upp við að byrja tengingu'
  }
}

onMounted(() => {
  // Only auto-start if email address is provided as prop
  if (props.emailAddress) {
    initialize(props.emailAddress, props.isSystemInbox)
  }
})

onUnmounted(() => {
  stopPolling()
  if (countdownInterval.value) {
    clearInterval(countdownInterval.value)
  }
})
</script>

