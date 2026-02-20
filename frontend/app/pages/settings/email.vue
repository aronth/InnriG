<template>
  <div class="container mx-auto px-4 py-8 max-w-4xl">
    <h1 class="text-3xl font-bold text-gray-900 mb-8">Tölvupóststillingar</h1>

    <!-- OAuth Email Connections Section -->
    <div class="bg-white rounded-lg shadow-md p-6 mb-6">
      <div class="flex items-center justify-between mb-4">
        <div>
          <h2 class="text-xl font-semibold text-gray-900">OAuth Tölvupóststillingar</h2>
          <p class="text-sm text-gray-600 mt-1">
            Tengdu netföng með Microsoft OAuth til að lesa og senda tölvupósta
          </p>
        </div>
        <button
          @click="showConnectModal = true"
          class="px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 flex items-center gap-2"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          Tengja netfang
        </button>
      </div>

      <!-- System Inbox Warning -->
      <div
        v-if="!hasSystemInbox"
        class="mb-4 p-3 bg-yellow-50 border border-yellow-200 rounded-md"
      >
        <div class="flex items-center gap-2">
          <svg class="w-5 h-5 text-yellow-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
          </svg>
          <p class="text-sm text-yellow-800">
            <strong>Kerfisinnhólf er ekki tengt.</strong> Tengdu kerfisinnhólf til að virkja tölvupóstsíðu.
          </p>
        </div>
      </div>

      <!-- Loading State -->
      <div v-if="loadingConnections" class="text-center py-8">
        <div class="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600"></div>
        <p class="mt-2 text-sm text-gray-600">Hleður tengingum...</p>
      </div>

      <!-- Connections List -->
      <div v-else-if="connections.length > 0" class="space-y-3">
        <div
          v-for="connection in connections"
          :key="connection.emailAddress"
          class="flex items-center justify-between p-4 border border-gray-200 rounded-lg hover:bg-gray-50"
        >
          <div class="flex-1">
            <div class="flex items-center gap-2">
              <span class="font-medium text-gray-900">
                {{ connection.emailAddress }}
              </span>
              <span
                v-if="connection.isSystemInbox"
                class="px-2 py-0.5 text-xs font-semibold bg-purple-100 text-purple-800 rounded-full"
              >
                Kerfisinnhólf
              </span>
              <span
                v-if="connection.isConnected"
                class="px-2 py-0.5 text-xs font-semibold bg-green-100 text-green-800 rounded-full"
              >
                Tengt
              </span>
              <span
                v-else
                class="px-2 py-0.5 text-xs font-semibold bg-gray-100 text-gray-800 rounded-full"
              >
                Ekki tengt
              </span>
            </div>
            <div v-if="connection.lastRefreshedAt" class="text-sm text-gray-600 mt-1">
              Síðast endurnýjað: {{ formatDate(connection.lastRefreshedAt) }}
            </div>
          </div>
          <div class="flex items-center gap-2">
            <button
              v-if="connection.isConnected && !connection.isSystemInbox"
              @click="handleSetSystemInbox(connection.emailAddress, true)"
              class="px-3 py-1 text-sm text-purple-600 hover:text-purple-800 hover:bg-purple-50 rounded"
              :disabled="updatingSystemInbox"
              title="Gera að kerfisinnhólfi"
            >
              <svg class="w-4 h-4 inline mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
              </svg>
              Gera kerfisinnhólf
            </button>
            <button
              v-if="connection.isConnected && connection.isSystemInbox"
              @click="handleSetSystemInbox(connection.emailAddress, false)"
              class="px-3 py-1 text-sm text-gray-600 hover:text-gray-800 hover:bg-gray-50 rounded"
              :disabled="updatingSystemInbox"
              title="Fjarlægja kerfisinnhólf"
            >
              <svg class="w-4 h-4 inline mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
              Fjarlægja kerfisinnhólf
            </button>
            <button
              v-if="connection.isConnected"
              @click="handleDisconnect(connection.emailAddress)"
              class="px-3 py-1 text-sm text-red-600 hover:text-red-800 hover:bg-red-50 rounded"
              :disabled="disconnecting"
            >
              Aftengja
            </button>
            <button
              v-else
              @click="handleConnect(connection.emailAddress, connection.isSystemInbox)"
              class="px-3 py-1 text-sm text-indigo-600 hover:text-indigo-800 hover:bg-indigo-50 rounded"
            >
              Tengja
            </button>
          </div>
        </div>
      </div>
      <div v-else class="text-center py-8 text-gray-500">
        <p>Engin netföng tengd</p>
        <p class="text-sm mt-2">Smelltu á "Tengja netfang" til að byrja</p>
      </div>
    </div>

    <!-- Email Mappings Section -->
    <div class="bg-white rounded-lg shadow-md p-6 mb-6">
      <div class="flex items-center justify-between mb-4">
        <h2 class="text-xl font-semibold text-gray-900">Tengd netföng</h2>
        <button
          @click="showAddEmailModal = true"
          class="px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 flex items-center gap-2"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          Bæta við netfangi
        </button>
      </div>

      <!-- Email Mappings List -->
      <div v-if="emailMappings.length > 0" class="space-y-3">
        <div
          v-for="mapping in emailMappings"
          :key="mapping.id"
          class="flex items-center justify-between p-4 border border-gray-200 rounded-lg hover:bg-gray-50"
        >
          <div class="flex-1">
            <div class="flex items-center gap-2">
              <span class="font-medium text-gray-900">
                {{ mapping.displayName || mapping.emailAddress }}
              </span>
              <span
                v-if="mapping.isDefault"
                class="px-2 py-0.5 text-xs font-semibold bg-indigo-100 text-indigo-800 rounded-full"
              >
                Sjálfgefið
              </span>
            </div>
            <div class="text-sm text-gray-600 mt-1">{{ mapping.emailAddress }}</div>
          </div>
          <div class="flex items-center gap-2">
            <button
              @click="editEmailMapping(mapping)"
              class="px-3 py-1 text-sm text-indigo-600 hover:text-indigo-800 hover:bg-indigo-50 rounded"
            >
              Breyta
            </button>
            <button
              @click="handleDeleteEmailMapping(mapping.id)"
              class="px-3 py-1 text-sm text-red-600 hover:text-red-800 hover:bg-red-50 rounded"
            >
              Eyða
            </button>
          </div>
        </div>
      </div>
      <div v-else class="text-center py-8 text-gray-500">
        <p>Engin netföng tengd við reikninginn</p>
        <p class="text-sm mt-2">Smelltu á "Bæta við netfangi" til að tengja netfang</p>
      </div>
    </div>

    <!-- Email Signature Section -->
    <div class="bg-white rounded-lg shadow-md p-6">
      <h2 class="text-xl font-semibold text-gray-900 mb-4">Tölvupóstundirskrift</h2>
      <p class="text-sm text-gray-600 mb-4">
        Undirskriftin verður sjálfkrafa bætt við í lok tölvupósta sem þú sendir.
      </p>

      <div class="mb-4">
        <label class="block text-sm font-medium text-gray-700 mb-2">
          Undirskrift
        </label>
        <div class="border border-gray-300 rounded-md" style="min-height: 200px;">
          <EmailEditor
            v-model="emailSignature"
            :editor-class="'min-h-[200px]'"
          />
        </div>
      </div>

      <div class="flex items-center justify-end gap-3">
        <button
          @click="loadEmailSignature"
          class="px-4 py-2 text-gray-700 bg-gray-100 rounded-md hover:bg-gray-200 focus:outline-none focus:ring-2 focus:ring-gray-500"
        >
          Hætta við
        </button>
        <button
          @click="saveEmailSignature"
          :disabled="savingSignature"
          class="px-4 py-2 text-white bg-indigo-600 rounded-md hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
        >
          <svg
            v-if="savingSignature"
            class="animate-spin h-5 w-5"
            xmlns="http://www.w3.org/2000/svg"
            fill="none"
            viewBox="0 0 24 24"
          >
            <circle
              class="opacity-25"
              cx="12"
              cy="12"
              r="10"
              stroke="currentColor"
              stroke-width="4"
            ></circle>
            <path
              class="opacity-75"
              fill="currentColor"
              d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
            ></path>
          </svg>
          {{ savingSignature ? 'Vista...' : 'Vista' }}
        </button>
      </div>
    </div>

    <!-- Add/Edit Email Modal -->
    <div
      v-if="showAddEmailModal || editingMapping"
      class="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50"
      @click.self="closeEmailModal"
    >
      <div class="bg-white rounded-lg shadow-xl w-full max-w-md m-4">
        <div class="px-6 py-4 border-b border-gray-200 flex items-center justify-between">
          <h3 class="text-lg font-bold text-gray-900">
            {{ editingMapping ? 'Breyta netfangi' : 'Bæta við netfangi' }}
          </h3>
          <button
            @click="closeEmailModal"
            class="text-gray-400 hover:text-gray-600 focus:outline-none"
          >
            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        <div class="px-6 py-4">
          <div class="mb-4">
            <label class="block text-sm font-medium text-gray-700 mb-2">
              Netfang *
            </label>
            <input
              v-model="emailForm.emailAddress"
              type="email"
              :disabled="editingMapping !== null"
              class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500 disabled:bg-gray-50 disabled:text-gray-600"
              placeholder="nafn@example.com"
            />
          </div>

          <div class="mb-4">
            <label class="block text-sm font-medium text-gray-700 mb-2">
              Birtingarnafn
            </label>
            <input
              v-model="emailForm.displayName"
              type="text"
              class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
              placeholder="Jón Jónsson"
            />
          </div>

          <div class="mb-4">
            <label class="flex items-center">
              <input
                v-model="emailForm.isDefault"
                type="checkbox"
                class="rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
              />
              <span class="ml-2 text-sm text-gray-700">Gera að sjálfgefnu netfangi</span>
            </label>
          </div>
        </div>

        <div class="px-6 py-4 border-t border-gray-200 flex items-center justify-end gap-3">
          <button
            @click="closeEmailModal"
            class="px-4 py-2 text-gray-700 bg-gray-100 rounded-md hover:bg-gray-200 focus:outline-none focus:ring-2 focus:ring-gray-500"
          >
            Hætta við
          </button>
          <button
            @click="saveEmailMapping"
            :disabled="savingEmail || !emailForm.emailAddress.trim()"
            class="px-4 py-2 text-white bg-indigo-600 rounded-md hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {{ savingEmail ? 'Vista...' : 'Vista' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Connect Email Modal -->
    <EmailConnectionModal
      v-if="showConnectModal"
      :email-address="connectModalEmail"
      :is-system-inbox="connectModalIsSystemInbox"
      @close="showConnectModal = false"
      @success="handleConnectSuccess"
    />
  </div>
</template>

<script setup lang="ts">
import type { UserEmailMappingDto } from '~/types/userEmailSettings'
import type { EmailConnectionStatusDto } from '~/types/email'

const { getEmailMappings, createEmailMapping, updateEmailMapping, deleteEmailMapping, getEmailSignature, updateEmailSignature } = useUserEmailSettings()
const { getConnectionStatus, disconnectEmail, setSystemInbox } = useEmailAuth()

const emailMappings = ref<UserEmailMappingDto[]>([])
const emailSignature = ref<string>('')
const loading = ref(true)
const savingSignature = ref(false)
const savingEmail = ref(false)
const showAddEmailModal = ref(false)
const editingMapping = ref<UserEmailMappingDto | null>(null)

// OAuth connection state
const connections = ref<EmailConnectionStatusDto[]>([])
const loadingConnections = ref(true)
const showConnectModal = ref(false)
const connectModalEmail = ref('')
const connectModalIsSystemInbox = ref(false)
const disconnecting = ref(false)
const updatingSystemInbox = ref(false)

const emailForm = ref({
  emailAddress: '',
  displayName: '',
  isDefault: false
})

const loadEmailMappings = async () => {
  try {
    emailMappings.value = await getEmailMappings()
  } catch (error) {
    console.error('Error loading email mappings:', error)
  }
}

const loadEmailSignature = async () => {
  try {
    emailSignature.value = await getEmailSignature()
  } catch (error) {
    console.error('Error loading email signature:', error)
  }
}

const saveEmailSignature = async () => {
  savingSignature.value = true
  try {
    await updateEmailSignature(emailSignature.value)
    // Show success message
  } catch (error) {
    console.error('Error saving email signature:', error)
    alert('Villa kom upp við að vista undirskrift')
  } finally {
    savingSignature.value = false
  }
}

const editEmailMapping = (mapping: UserEmailMappingDto) => {
  editingMapping.value = mapping
  emailForm.value = {
    emailAddress: mapping.emailAddress,
    displayName: mapping.displayName || '',
    isDefault: mapping.isDefault
  }
}

const closeEmailModal = () => {
  showAddEmailModal.value = false
  editingMapping.value = null
  emailForm.value = {
    emailAddress: '',
    displayName: '',
    isDefault: false
  }
}

const saveEmailMapping = async () => {
  if (!emailForm.value.emailAddress.trim()) return

  savingEmail.value = true
  try {
    if (editingMapping.value) {
      await updateEmailMapping(editingMapping.value.id, {
        displayName: emailForm.value.displayName || undefined,
        isDefault: emailForm.value.isDefault
      })
    } else {
      await createEmailMapping({
        emailAddress: emailForm.value.emailAddress,
        displayName: emailForm.value.displayName || undefined,
        isDefault: emailForm.value.isDefault
      })
    }
    await loadEmailMappings()
    closeEmailModal()
  } catch (error) {
    console.error('Error saving email mapping:', error)
    alert('Villa kom upp við að vista netfang')
  } finally {
    savingEmail.value = false
  }
}

const handleDeleteEmailMapping = async (id: string) => {
  if (!confirm('Ertu viss um að þú viljir eyða þessu netfangi?')) return

  try {
    await deleteEmailMapping(id)
    await loadEmailMappings()
  } catch (error) {
    console.error('Error deleting email mapping:', error)
    alert('Villa kom upp við að eyða netfangi')
  }
}

// OAuth connection functions
const loadConnections = async () => {
  try {
    loadingConnections.value = true
    connections.value = await getConnectionStatus()
  } catch (error) {
    console.error('Error loading connections:', error)
  } finally {
    loadingConnections.value = false
  }
}

const hasSystemInbox = computed(() => {
  return connections.value.some(c => c.isSystemInbox && c.isConnected)
})

const handleConnect = (emailAddress: string, isSystemInbox: boolean) => {
  connectModalEmail.value = emailAddress
  connectModalIsSystemInbox.value = isSystemInbox
  showConnectModal.value = true
}

const handleDisconnect = async (emailAddress: string) => {
  if (!confirm(`Ertu viss um að þú viljir aftengja ${emailAddress}?`)) return

  disconnecting.value = true
  try {
    await disconnectEmail(emailAddress)
    await loadConnections()
  } catch (error) {
    console.error('Error disconnecting email:', error)
    alert('Villa kom upp við að aftengja netfangi')
  } finally {
    disconnecting.value = false
  }
}

const handleConnectSuccess = async (emailAddress: string) => {
  showConnectModal.value = false
  connectModalEmail.value = ''
  connectModalIsSystemInbox.value = false
  await loadConnections()
}

const handleSetSystemInbox = async (emailAddress: string, isSystemInbox: boolean) => {
  const action = isSystemInbox ? 'gera að kerfisinnhólfi' : 'fjarlægja kerfisinnhólf'
  if (!confirm(`Ertu viss um að þú viljir ${action} ${emailAddress}?`)) return

  updatingSystemInbox.value = true
  try {
    await setSystemInbox(emailAddress, isSystemInbox)
    await loadConnections()
  } catch (error) {
    console.error('Error setting system inbox:', error)
    alert('Villa kom upp við að uppfæra kerfisinnhólf')
  } finally {
    updatingSystemInbox.value = false
  }
}

const formatDate = (dateString: string) => {
  try {
    const date = new Date(dateString)
    return new Intl.DateTimeFormat('is-IS', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    }).format(date)
  } catch {
    return dateString
  }
}

onMounted(async () => {
  loading.value = true
  await Promise.all([loadEmailMappings(), loadEmailSignature(), loadConnections()])
  loading.value = false
})
</script>

