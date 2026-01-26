<template>
  <div
    v-if="show"
    class="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50"
    @click.self="handleCancel"
  >
    <div class="bg-white rounded-lg shadow-xl w-full max-w-4xl max-h-[90vh] flex flex-col m-4">
      <!-- Header -->
      <div class="px-6 py-4 border-b border-gray-200 flex items-center justify-between">
        <h2 class="text-xl font-bold text-gray-900">Svara tölvupósti</h2>
        <button
          @click="handleCancel"
          class="text-gray-400 hover:text-gray-600 focus:outline-none"
        >
          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>

      <!-- Content -->
      <div class="flex-1 overflow-y-auto px-6 py-4">
        <!-- From Email Selection -->
        <div class="mb-4">
          <label class="block text-sm font-medium text-gray-700 mb-2">
            Sendir frá
          </label>
          <select
            v-model="selectedFromEmail"
            class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
          >
            <option
              v-for="mapping in emailMappings"
              :key="mapping.id"
              :value="mapping.emailAddress"
            >
              {{ mapping.displayName || mapping.emailAddress }} ({{ mapping.emailAddress }})
            </option>
          </select>
        </div>

        <!-- To -->
        <div class="mb-4">
          <label class="block text-sm font-medium text-gray-700 mb-2">
            Til
          </label>
          <input
            type="text"
            :value="toEmail"
            disabled
            class="w-full px-3 py-2 border border-gray-300 rounded-md bg-gray-50 text-gray-600"
          />
        </div>

        <!-- Subject -->
        <div class="mb-4">
          <label class="block text-sm font-medium text-gray-700 mb-2">
            Efni
          </label>
          <input
            v-model="subject"
            type="text"
            class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
          />
        </div>

        <!-- CC/BCC -->
        <div class="mb-4 grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-2">
              CC
            </label>
            <input
              v-model="cc"
              type="text"
              placeholder="netfang1@example.com, netfang2@example.com"
              class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
            />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-2">
              BCC
            </label>
            <input
              v-model="bcc"
              type="text"
              placeholder="netfang1@example.com, netfang2@example.com"
              class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
            />
          </div>
        </div>

        <!-- Email Editor -->
        <div class="mb-4">
          <label class="block text-sm font-medium text-gray-700 mb-2">
            Skilaboð
          </label>
          <div class="border border-gray-300 rounded-md" style="min-height: 300px;">
            <EmailEditor
              ref="editorRef"
              v-model="body"
              :editor-class="'min-h-[300px]'"
            />
          </div>
        </div>
      </div>

      <!-- Footer -->
      <div class="px-6 py-4 border-t border-gray-200 flex items-center justify-end gap-3">
        <button
          @click="handleCancel"
          class="px-4 py-2 text-gray-700 bg-gray-100 rounded-md hover:bg-gray-200 focus:outline-none focus:ring-2 focus:ring-gray-500"
        >
          Hætta við
        </button>
        <button
          @click="handleSend"
          :disabled="sending || !body.trim() || !selectedFromEmail"
          class="px-4 py-2 text-white bg-indigo-600 rounded-md hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
        >
          <svg
            v-if="sending"
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
          {{ sending ? 'Sendi...' : 'Senda' }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { UserEmailMappingDto } from '~/types/userEmailSettings'

const props = defineProps<{
  show: boolean
  toEmail: string
  toName: string
  subject: string
  draftBody?: string
  emailMappings: UserEmailMappingDto[]
  emailSignature?: string
}>()

const emit = defineEmits<{
  'update:show': [value: boolean]
  'send': [data: { body: string; fromEmailAddress: string; cc?: string; bcc?: string }]
  'cancel': []
}>()

const selectedFromEmail = ref<string>('')
const body = ref<string>('')
const cc = ref<string>('')
const bcc = ref<string>('')
const subject = ref<string>('')
const sending = ref(false)
const editorRef = ref<InstanceType<typeof EmailEditor> | null>(null)

// Set default email mapping
watch(() => props.emailMappings, (mappings) => {
  if (mappings.length > 0 && !selectedFromEmail.value) {
    const defaultMapping = mappings.find(m => m.isDefault) || mappings[0]
    selectedFromEmail.value = defaultMapping.emailAddress
  }
}, { immediate: true })

// Set initial body and subject
watch(() => props.show, (isShowing) => {
  if (isShowing) {
    // Combine draft body and signature
    let combinedBody = props.draftBody || ''
    if (props.emailSignature && props.emailSignature.trim()) {
      if (combinedBody.trim()) {
        // Convert HTML signature to plain text if draft is plain text, or keep HTML if draft is HTML
        const signature = props.emailSignature
        // If draft body contains HTML tags, append signature as HTML, otherwise as plain text
        if (combinedBody.includes('<') && combinedBody.includes('>')) {
          combinedBody += '<br><br>' + signature
        } else {
          combinedBody += '\n\n' + (signature.replace(/<[^>]*>/g, '')) // Strip HTML tags for plain text
        }
      } else {
        combinedBody = props.emailSignature
      }
    }
    body.value = combinedBody
    subject.value = props.subject
    cc.value = ''
    bcc.value = ''
    
    // Wait for next tick to ensure editor is mounted, then set content
    nextTick(() => {
      if (editorRef.value) {
        editorRef.value.setContent(combinedBody)
      }
    })
  }
}, { immediate: true })

watch(() => props.subject, (newSubject) => {
  if (props.show) {
    subject.value = newSubject
  }
})

const handleSend = () => {
  if (!body.value.trim() || !selectedFromEmail.value) return

  sending.value = true
  emit('send', {
    body: body.value,
    fromEmailAddress: selectedFromEmail.value,
    cc: cc.value.trim() || undefined,
    bcc: bcc.value.trim() || undefined
  })
  // Note: sending will be set to false by parent component after API call completes
}

const handleCancel = () => {
  emit('update:show', false)
  emit('cancel')
  body.value = ''
  cc.value = ''
  bcc.value = ''
  sending.value = false
}

// Expose method to set sending state (called by parent after API call)
defineExpose({
  setSending: (value: boolean) => {
    sending.value = value
  }
})
</script>

