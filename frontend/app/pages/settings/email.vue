<template>
  <div class="container mx-auto px-4 py-8 max-w-4xl">
    <h1 class="text-3xl font-bold text-gray-900 mb-8">Tölvupóststillingar</h1>

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
  </div>
</template>

<script setup lang="ts">
import type { UserEmailMappingDto } from '~/types/userEmailSettings'

const { getEmailMappings, createEmailMapping, updateEmailMapping, deleteEmailMapping, getEmailSignature, updateEmailSignature } = useUserEmailSettings()

const emailMappings = ref<UserEmailMappingDto[]>([])
const emailSignature = ref<string>('')
const loading = ref(true)
const savingSignature = ref(false)
const savingEmail = ref(false)
const showAddEmailModal = ref(false)
const editingMapping = ref<UserEmailMappingDto | null>(null)

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

onMounted(async () => {
  loading.value = true
  await Promise.all([loadEmailMappings(), loadEmailSignature()])
  loading.value = false
})
</script>

