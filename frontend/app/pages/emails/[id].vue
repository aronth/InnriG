<template>
  <div class="min-h-[80vh]">
    <div class="mb-6 flex items-center justify-between">
      <div>
        <button
          @click="navigateTo('/emails')"
          class="text-indigo-600 hover:text-indigo-800 mb-2 flex items-center gap-2"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
          </svg>
          Til baka
        </button>
        <h1 class="text-3xl font-bold text-gray-800">{{ conversation?.subject || 'Hleður...' }}</h1>
        <p class="text-gray-600 mt-1">{{ conversation?.fromName }} &lt;{{ conversation?.fromEmail }}&gt;</p>
      </div>
      <div class="flex gap-2">
        <button
          @click="reparseConversation"
          :disabled="reparsing"
          class="px-4 py-2 bg-purple-600 text-white rounded-md hover:bg-purple-700 focus:outline-none focus:ring-2 focus:ring-purple-500 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
        >
          <svg
            v-if="reparsing"
            class="animate-spin h-4 w-4"
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
          <svg
            v-else
            class="h-4 w-4"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"
            />
          </svg>
          {{ reparsing ? 'Endurflokkar...' : 'Endurflokka' }}
        </button>
        <select
          v-model="selectedStatus"
          @change="updateStatus"
          class="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
        >
          <option value="New">Ný</option>
          <option value="InProgress">Í vinnslu</option>
          <option value="AwaitingResponse">Bíður svars</option>
          <option value="Resolved">Leyst</option>
          <option value="Archived">Geymt</option>
        </select>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="text-center py-12">
      <div class="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600"></div>
      <p class="mt-2 text-gray-600">Hleður...</p>
    </div>

    <!-- Conversation Content -->
    <div v-else-if="conversation" class="space-y-6">
      <!-- Extracted Data -->
      <div v-if="conversation.extractedData" class="bg-gradient-to-br from-indigo-50 to-purple-50 rounded-lg p-6 border border-indigo-200">
        <h2 class="text-xl font-bold text-gray-800 mb-4">Útdregin upplýsingar</h2>
        <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
          <div v-if="conversation.extractedData.contactName">
            <div class="text-sm text-gray-600">Nafn</div>
            <div class="font-medium">{{ conversation.extractedData.contactName }}</div>
          </div>
          <div v-if="conversation.extractedData.requestedDate">
            <div class="text-sm text-gray-600">Dagsetning</div>
            <div class="font-medium">{{ formatDate(conversation.extractedData.requestedDate) }}</div>
          </div>
          <div v-if="conversation.extractedData.requestedTime">
            <div class="text-sm text-gray-600">Tími</div>
            <div class="font-medium">{{ formatTime(conversation.extractedData.requestedTime) }}</div>
          </div>
          <div v-if="conversation.extractedData.guestCount">
            <div class="text-sm text-gray-600">Fjöldi gesta</div>
            <div class="font-medium">{{ conversation.extractedData.guestCount }}</div>
          </div>
          <div v-if="conversation.extractedData.locationCode">
            <div class="text-sm text-gray-600">Staðsetning</div>
            <div class="font-medium">{{ conversation.extractedData.locationCode }}</div>
          </div>
          <div v-if="conversation.extractedData.specialRequests" class="col-span-2 md:col-span-4">
            <div class="text-sm text-gray-600">Sérstakar beiðnir</div>
            <div class="font-medium">{{ conversation.extractedData.specialRequests }}</div>
          </div>
        </div>
        <div class="mt-4 text-sm text-gray-500">
          Flokkun: <span class="font-medium">{{ conversation.extractedData.classification }}</span>
          (Áreiðanleiki: {{ (conversation.extractedData.confidence * 100).toFixed(0) }}%)
        </div>
      </div>

      <!-- Messages -->
      <div class="bg-white rounded-lg shadow-md overflow-hidden">
        <div class="px-6 py-4 bg-gray-50 border-b border-gray-200">
          <h2 class="text-xl font-bold text-gray-800">Skilaboð ({{ conversation.messages?.length || 0 }})</h2>
        </div>
        <div class="divide-y divide-gray-200">
          <div
            v-for="message in conversation.messages"
            :key="message.id"
            :class="[
              'p-6 hover:bg-gray-50',
              message.isAIResponse ? 'bg-blue-50 border-l-4 border-blue-500' : ''
            ]"
          >
            <div class="flex items-start justify-between mb-2">
              <div class="flex items-center gap-2">
                <div>
                  <div class="font-medium text-gray-900">{{ message.fromName || message.fromEmail }}</div>
                  <div class="text-sm text-gray-500">{{ message.fromEmail }}</div>
                </div>
                <span
                  v-if="message.isAIResponse"
                  class="px-2 py-1 bg-blue-100 text-blue-800 text-xs font-semibold rounded"
                >
                  AI Analysis
                </span>
              </div>
              <div class="flex items-center gap-2">
                <button
                  v-if="!message.isAIResponse"
                  @click="regenerateAI(message.id)"
                  :disabled="regeneratingAI === message.id"
                  class="text-xs px-2 py-1 bg-purple-100 text-purple-700 rounded hover:bg-purple-200 disabled:opacity-50"
                  title="Endurnýja AI greiningu"
                >
                  <span v-if="regeneratingAI === message.id">Endurnýjar...</span>
                  <span v-else>Endurnýja AI</span>
                </button>
                <div class="text-sm text-gray-500">
                  {{ formatDate(message.receivedDateTime) }}
                </div>
              </div>
            </div>
            <div v-if="!message.isAIResponse" class="mb-2">
              <span class="text-sm font-medium text-gray-700">Til:</span>
              <span class="text-sm text-gray-600 ml-2">{{ message.toName || message.toEmail }}</span>
            </div>
            <div class="mb-4">
              <span class="text-lg font-semibold text-gray-900">{{ message.subject }}</span>
            </div>
            <!-- AI Message Body -->
            <div v-if="message.isAIResponse && message.messageBody" class="mb-4">
              <div class="prose prose-sm max-w-none bg-white p-4 rounded border border-blue-200 whitespace-pre-wrap">
                {{ message.messageBody }}
              </div>
            </div>
            <!-- Regular Message Body Toggle -->
            <div v-else-if="!message.isAIResponse" class="mb-4">
              <button
                @click="loadMessageBody(message.id)"
                class="text-indigo-600 hover:text-indigo-800 text-sm font-medium"
              >
                {{ messageBodies[message.id] ? 'Fela' : 'Sýna' }} efni
              </button>
            </div>
            <div
              v-if="messageBodies[message.id] && !message.isAIResponse"
              class="prose prose-sm max-w-none bg-gray-50 p-4 rounded border border-gray-200"
              v-html="messageBodies[message.id]?.html || messageBodies[message.id]?.text"
            ></div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { EmailConversationDto, EmailMessageBodyDto } from '~/types/email'

const route = useRoute()
const { getConversation, getMessageBody, updateStatus: updateStatusApi, reparseConversation: reparseConversationApi, regenerateAIAnalysis } = useEmails()

const conversation = ref<EmailConversationDto | null>(null)
const loading = ref(true)
const messageBodies = ref<Record<string, EmailMessageBodyDto>>({})
const selectedStatus = ref('')
const reparsing = ref(false)
const regeneratingAI = ref<string | null>(null)

const loadConversation = async () => {
  loading.value = true
  try {
    const id = route.params.id as string
    conversation.value = await getConversation(id)
    if (conversation.value) {
      selectedStatus.value = conversation.value.status
    }
  } catch (error) {
    console.error('Error loading conversation:', error)
  } finally {
    loading.value = false
  }
}

const loadMessageBody = async (messageId: string) => {
  if (messageBodies.value[messageId]) {
    delete messageBodies.value[messageId]
    return
  }

  try {
    const body = await getMessageBody(messageId)
    messageBodies.value[messageId] = body
  } catch (error) {
    console.error('Error loading message body:', error)
  }
}

const updateStatus = async () => {
  if (!conversation.value) return

  try {
    await updateStatusApi(conversation.value.id, selectedStatus.value)
    if (conversation.value) {
      conversation.value.status = selectedStatus.value
    }
  } catch (error) {
    console.error('Error updating status:', error)
  }
}

const reparseConversation = async () => {
  if (!conversation.value) return

  reparsing.value = true
  try {
    await reparseConversationApi(conversation.value.id)
    // Reload conversation after a short delay to allow classification to process
    setTimeout(() => {
      loadConversation()
    }, 2000)
  } catch (error) {
    console.error('Error re-parsing conversation:', error)
    alert('Villa kom upp við endurflokkun. Reyndu aftur síðar.')
  } finally {
    reparsing.value = false
  }
}

const formatDate = (dateString: string) => {
  const date = new Date(dateString)
  return new Intl.DateTimeFormat('is-IS', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  }).format(date)
}

const formatTime = (timeString: string) => {
  const time = timeString.split(':')
  return `${time[0]}:${time[1]}`
}

const regenerateAI = async (messageId: string) => {
  regeneratingAI.value = messageId
  try {
    const updated = await regenerateAIAnalysis(messageId)
    conversation.value = updated
  } catch (error) {
    console.error('Error regenerating AI analysis:', error)
    alert('Villa kom upp við endurnýjun AI greiningar. Reyndu aftur síðar.')
  } finally {
    regeneratingAI.value = null
  }
}

onMounted(() => {
  loadConversation()
})
</script>

