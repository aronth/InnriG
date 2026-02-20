<template>
  <div class="container mx-auto px-4 py-8 max-w-6xl">
    <div class="mb-8">
      <h1 class="text-3xl font-bold text-gray-900">Tölvupóstflokkun</h1>
      <p class="mt-2 text-sm text-gray-600">Stjórnaðu flokkum fyrir tölvupósta</p>
    </div>

    <!-- Filters and Actions -->
    <div class="bg-white rounded-lg shadow-md p-6 mb-6">
      <div class="flex items-center justify-between mb-4">
        <div class="flex items-center gap-4">
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Leita að nafni..."
            class="px-4 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
          />
          <select
            v-model="filterActive"
            class="px-4 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
          >
            <option :value="null">Allir</option>
            <option :value="true">Virkir</option>
            <option :value="false">Óvirkir</option>
          </select>
          <select
            v-model="filterSystem"
            class="px-4 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
          >
            <option :value="null">Allir</option>
            <option :value="true">Kerfisflokkar</option>
            <option :value="false">Notendaskilgreindir</option>
          </select>
        </div>
        <button
          @click="showCreateModal = true"
          class="px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 flex items-center gap-2"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          Bæta við flokki
        </button>
      </div>
    </div>

    <!-- Classifications Table -->
    <div class="bg-white rounded-lg shadow-md overflow-hidden">
      <table class="min-w-full divide-y divide-gray-200">
        <thead class="bg-gray-50">
          <tr>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Nafn</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Lýsing</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Tegund</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Staða</th>
            <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Aðgerðir</th>
          </tr>
        </thead>
        <tbody class="bg-white divide-y divide-gray-200">
          <tr v-for="classification in filteredClassifications" :key="classification.id">
            <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
              {{ classification.name }}
            </td>
            <td class="px-6 py-4 text-sm text-gray-500">
              {{ classification.description }}
            </td>
            <td class="px-6 py-4 whitespace-nowrap">
              <span
                v-if="classification.isSystem"
                class="px-2 py-1 text-xs font-semibold rounded-full bg-purple-100 text-purple-800"
              >
                Kerfisflokkur
              </span>
              <span
                v-else
                class="px-2 py-1 text-xs font-semibold rounded-full bg-blue-100 text-blue-800"
              >
                Notendaskilgreindur
              </span>
            </td>
            <td class="px-6 py-4 whitespace-nowrap">
              <span
                :class="classification.isActive ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'"
                class="px-2 py-1 text-xs font-semibold rounded-full"
              >
                {{ classification.isActive ? 'Virkur' : 'Óvirkur' }}
              </span>
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
              <button
                @click="editClassification(classification)"
                class="text-indigo-600 hover:text-indigo-900 mr-4"
              >
                Breyta
              </button>
              <button
                @click="handleDelete(classification)"
                class="text-red-600 hover:text-red-900"
              >
                Eyða
              </button>
            </td>
          </tr>
          <tr v-if="filteredClassifications.length === 0">
            <td colspan="5" class="px-6 py-4 text-center text-sm text-gray-500">
              Engin flokkun fannst
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Create/Edit Modal -->
    <div
      v-if="showCreateModal || editingClassification"
      class="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50"
      @click.self="closeModal"
    >
      <div class="relative top-20 mx-auto p-5 border w-11/12 max-w-2xl shadow-lg rounded-md bg-white">
        <div class="mt-3">
          <h3 class="text-lg font-medium text-gray-900 mb-4">
            {{ editingClassification ? 'Breyta flokki' : 'Bæta við flokki' }}
          </h3>
          <form @submit.prevent="saveClassification">
            <div class="space-y-4">
              <div>
                <label class="block text-sm font-medium text-gray-700">Nafn *</label>
                <input
                  v-model="form.name"
                  type="text"
                  required
                  class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
                />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700">Lýsing *</label>
                <textarea
                  v-model="form.description"
                  required
                  rows="3"
                  class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
                />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700">Kerfisprompt (valfrjálst)</label>
                <textarea
                  v-model="form.systemPrompt"
                  rows="5"
                  placeholder="Sérsniðið prompt fyrir AI. Ef eftirtekið, verður notað staðlað prompt."
                  class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
                />
                <p class="mt-1 text-xs text-gray-500">Ef eftirtekið, verður notað staðlað prompt með flokkum.</p>
              </div>
              <div v-if="editingClassification">
                <label class="flex items-center">
                  <input
                    v-model="form.isActive"
                    type="checkbox"
                    class="rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
                  />
                  <span class="ml-2 text-sm text-gray-700">Virkur</span>
                </label>
              </div>
            </div>
            <div class="mt-6 flex items-center justify-end gap-3">
              <button
                type="button"
                @click="closeModal"
                class="px-4 py-2 text-gray-700 bg-gray-100 rounded-md hover:bg-gray-200"
              >
                Hætta við
              </button>
              <button
                type="submit"
                :disabled="saving"
                class="px-4 py-2 text-white bg-indigo-600 rounded-md hover:bg-indigo-700 disabled:opacity-50"
              >
                {{ saving ? 'Vista...' : 'Vista' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { EmailClassification, CreateEmailClassificationDto, UpdateEmailClassificationDto } from '~/types/emailClassification'

const { getAll, create, update, delete: deleteClassification } = useEmailClassifications()

const classifications = ref<EmailClassification[]>([])
const loading = ref(true)
const saving = ref(false)
const showCreateModal = ref(false)
const editingClassification = ref<EmailClassification | null>(null)
const searchQuery = ref('')
const filterActive = ref<boolean | null>(null)
const filterSystem = ref<boolean | null>(null)

const form = ref<CreateEmailClassificationDto & { isActive?: boolean }>({
  name: '',
  description: '',
  systemPrompt: null,
  isActive: true
})

const filteredClassifications = computed(() => {
  let result = classifications.value

  if (searchQuery.value) {
    const query = searchQuery.value.toLowerCase()
    result = result.filter(c => c.name.toLowerCase().includes(query) || c.description.toLowerCase().includes(query))
  }

  if (filterActive.value !== null) {
    result = result.filter(c => c.isActive === filterActive.value)
  }

  if (filterSystem.value !== null) {
    result = result.filter(c => c.isSystem === filterSystem.value)
  }

  return result
})

const loadClassifications = async () => {
  try {
    loading.value = true
    classifications.value = await getAll()
  } catch (error) {
    console.error('Error loading classifications:', error)
    alert('Villa kom upp við að sækja flokka')
  } finally {
    loading.value = false
  }
}

const editClassification = (classification: EmailClassification) => {
  editingClassification.value = classification
  form.value = {
    name: classification.name,
    description: classification.description,
    systemPrompt: classification.systemPrompt || null,
    isActive: classification.isActive
  }
}

const handleDelete = async (classification: EmailClassification) => {
  if (!confirm(`Ertu viss um að þú viljir eyða flokknum "${classification.name}"?`)) {
    return
  }

  try {
    await deleteClassification(classification.id)
    await loadClassifications()
  } catch (error: any) {
    console.error('Error deleting classification:', error)
    alert(error.data?.error || error.message || 'Villa kom upp við að eyða flokki')
  }
}

const saveClassification = async () => {
  try {
    saving.value = true

    if (editingClassification.value) {
      const updateDto: UpdateEmailClassificationDto = {
        name: form.value.name,
        description: form.value.description,
        systemPrompt: form.value.systemPrompt || null,
        isActive: form.value.isActive ?? true
      }
      await update(editingClassification.value.id, updateDto)
    } else {
      const createDto: CreateEmailClassificationDto = {
        name: form.value.name,
        description: form.value.description,
        systemPrompt: form.value.systemPrompt || null
      }
      await create(createDto)
    }

    await loadClassifications()
    closeModal()
  } catch (error: any) {
    console.error('Error saving classification:', error)
    alert(error.data?.error || error.message || 'Villa kom upp við að vista flokk')
  } finally {
    saving.value = false
  }
}

const closeModal = () => {
  showCreateModal.value = false
  editingClassification.value = null
  form.value = {
    name: '',
    description: '',
    systemPrompt: null,
    isActive: true
  }
}

onMounted(() => {
  loadClassifications()
})
</script>

