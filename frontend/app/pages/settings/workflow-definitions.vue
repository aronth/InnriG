<template>
  <div class="container mx-auto px-4 py-8 max-w-6xl">
    <div class="mb-8">
      <h1 class="text-3xl font-bold text-gray-900">Ferlar</h1>
      <p class="mt-2 text-sm text-gray-600">Stjórnaðu ferlum fyrir tölvupósta</p>
    </div>

    <!-- Actions -->
    <div class="bg-white rounded-lg shadow-md p-6 mb-6">
      <div class="flex items-center justify-between">
        <input
          v-model="searchQuery"
          type="text"
          placeholder="Leita að nafni..."
          class="px-4 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
        />
        <button
          @click="showCreateModal = true"
          class="px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 flex items-center gap-2"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          Bæta við ferli
        </button>
      </div>
    </div>

    <!-- Workflows Table -->
    <div class="bg-white rounded-lg shadow-md overflow-hidden">
      <table class="min-w-full divide-y divide-gray-200">
        <thead class="bg-gray-50">
          <tr>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Nafn</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Tengdur flokkur</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Skref</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Staða</th>
            <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Aðgerðir</th>
          </tr>
        </thead>
        <tbody class="bg-white divide-y divide-gray-200">
          <tr v-for="workflow in filteredWorkflows" :key="workflow.id">
            <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
              {{ workflow.name }}
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
              {{ workflow.classification?.name || 'Enginn (handvirkur)' }}
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
              {{ workflow.steps.length }} skref
            </td>
            <td class="px-6 py-4 whitespace-nowrap">
              <span
                :class="workflow.isActive ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'"
                class="px-2 py-1 text-xs font-semibold rounded-full"
              >
                {{ workflow.isActive ? 'Virkur' : 'Óvirkur' }}
              </span>
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
              <button
                @click="editWorkflow(workflow)"
                class="text-indigo-600 hover:text-indigo-900 mr-4"
              >
                Breyta
              </button>
              <button
                @click="deleteWorkflow(workflow)"
                class="text-red-600 hover:text-red-900"
              >
                Eyða
              </button>
            </td>
          </tr>
          <tr v-if="filteredWorkflows.length === 0">
            <td colspan="5" class="px-6 py-4 text-center text-sm text-gray-500">
              Engir ferlar fundust
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Create/Edit Modal -->
    <div
      v-if="showCreateModal || editingWorkflow"
      class="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50"
      @click.self="closeModal"
    >
      <div class="relative top-10 mx-auto p-5 border w-11/12 max-w-4xl shadow-lg rounded-md bg-white max-h-[90vh] overflow-y-auto">
        <div class="mt-3">
          <h3 class="text-lg font-medium text-gray-900 mb-4">
            {{ editingWorkflow ? 'Breyta ferli' : 'Bæta við ferli' }}
          </h3>
          <form @submit.prevent="saveWorkflow">
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
                <label class="block text-sm font-medium text-gray-700">Tengdur flokkur (valfrjálst)</label>
                <select
                  v-model="form.classificationId"
                  class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-indigo-500 focus:border-indigo-500"
                >
                  <option :value="null">Enginn (handvirkur)</option>
                  <option
                    v-for="classification in classifications"
                    :key="classification.id"
                    :value="classification.id"
                  >
                    {{ classification.name }}
                  </option>
                </select>
                <p class="mt-1 text-xs text-gray-500">Ef valið, verður ferlinn sjálfkrafa startaður þegar tölvupóstur er flokkaður sem þessi flokkur.</p>
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-2">Skref</label>
                <div class="space-y-2">
                  <div
                    v-for="(step, index) in form.steps"
                    :key="index"
                    class="border border-gray-300 rounded-md p-4"
                  >
                    <div class="grid grid-cols-2 gap-4 mb-2">
                      <div>
                        <label class="block text-xs font-medium text-gray-700">Skrefategund *</label>
                        <select
                          v-model="step.stepType"
                          required
                          @change="updateHandlerType(index)"
                          class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm text-sm"
                        >
                          <option value="">Veldu skrefategund</option>
                          <option
                            v-for="handler in stepHandlers"
                            :key="handler.handlerType"
                            :value="handler.stepType"
                          >
                            {{ handler.stepType }}
                          </option>
                        </select>
                      </div>
                      <div>
                        <label class="block text-xs font-medium text-gray-700">Röð *</label>
                        <input
                          v-model.number="step.order"
                          type="number"
                          required
                          min="1"
                          class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm text-sm"
                        />
                      </div>
                    </div>
                    <div class="mb-2">
                      <label class="block text-xs font-medium text-gray-700">Handler Type *</label>
                      <input
                        v-model="step.handlerType"
                        type="text"
                        required
                        placeholder="InnriGreifi.API.Services.Steps.OrderLookupStepHandler"
                        class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm text-sm"
                      />
                    </div>
                    <div class="mb-2">
                      <label class="flex items-center">
                        <input
                          v-model="step.requiresApproval"
                          type="checkbox"
                          class="rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
                        />
                        <span class="ml-2 text-xs text-gray-700">Krefst samþykkis</span>
                      </label>
                    </div>
                    <button
                      type="button"
                      @click="removeStep(index)"
                      class="text-xs text-red-600 hover:text-red-900"
                    >
                      Fjarlægja skref
                    </button>
                  </div>
                  <button
                    type="button"
                    @click="addStep"
                    class="text-sm text-indigo-600 hover:text-indigo-900"
                  >
                    + Bæta við skrefi
                  </button>
                </div>
              </div>
              <div v-if="editingWorkflow">
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
import type { WorkflowDefinition, CreateWorkflowDefinitionDto, UpdateWorkflowDefinitionDto, WorkflowStepDefinition, StepHandlerInfo } from '~/types/workflowDefinition'
import type { EmailClassification } from '~/types/emailClassification'

const { getAll, create, update, delete: deleteWorkflow, getStepHandlers } = useWorkflowDefinitions()
const { getAll: getAllClassifications } = useEmailClassifications()

const workflows = ref<WorkflowDefinition[]>([])
const classifications = ref<EmailClassification[]>([])
const stepHandlers = ref<StepHandlerInfo[]>([])
const loading = ref(true)
const saving = ref(false)
const showCreateModal = ref(false)
const editingWorkflow = ref<WorkflowDefinition | null>(null)
const searchQuery = ref('')

const form = ref<CreateWorkflowDefinitionDto & { isActive?: boolean }>({
  name: '',
  classificationId: null,
  steps: [],
  isActive: true
})

const filteredWorkflows = computed(() => {
  if (!searchQuery.value) return workflows.value
  const query = searchQuery.value.toLowerCase()
  return workflows.value.filter(w => w.name.toLowerCase().includes(query))
})

const loadWorkflows = async () => {
  try {
    loading.value = true
    workflows.value = await getAll()
  } catch (error) {
    console.error('Error loading workflows:', error)
    alert('Villa kom upp við að sækja ferla')
  } finally {
    loading.value = false
  }
}

const loadClassifications = async () => {
  try {
    classifications.value = await getAllClassifications()
  } catch (error) {
    console.error('Error loading classifications:', error)
  }
}

const loadStepHandlers = async () => {
  try {
    stepHandlers.value = await getStepHandlers()
  } catch (error) {
    console.error('Error loading step handlers:', error)
  }
}

const editWorkflow = (workflow: WorkflowDefinition) => {
  editingWorkflow.value = workflow
  form.value = {
    name: workflow.name,
    classificationId: workflow.classificationId || null,
    steps: workflow.steps.map(s => ({ ...s })),
    isActive: workflow.isActive
  }
}

const deleteWorkflowHandler = async (workflow: WorkflowDefinition) => {
  if (!confirm(`Ertu viss um að þú viljir eyða ferlinum "${workflow.name}"?`)) {
    return
  }

  try {
    await deleteWorkflow(workflow.id)
    await loadWorkflows()
  } catch (error: any) {
    console.error('Error deleting workflow:', error)
    alert(error.data?.error || error.message || 'Villa kom upp við að eyða ferli')
  }
}

const addStep = () => {
  form.value.steps.push({
    stepType: '',
    handlerType: '',
    order: form.value.steps.length + 1,
    requiresApproval: false,
    configuration: {}
  })
}

const removeStep = (index: number) => {
  form.value.steps.splice(index, 1)
  // Reorder steps
  form.value.steps.forEach((step, i) => {
    step.order = i + 1
  })
}

const updateHandlerType = (index: number) => {
  const step = form.value.steps[index]
  if (step.stepType) {
    const handler = stepHandlers.value.find(h => h.stepType === step.stepType)
    if (handler) {
      step.handlerType = handler.handlerType
    }
  }
}

const saveWorkflow = async () => {
  try {
    saving.value = true

    // Ensure all steps have handler types
    form.value.steps.forEach(step => {
      if (step.stepType && !step.handlerType) {
        updateHandlerType(form.value.steps.indexOf(step))
      }
    })

    if (editingWorkflow.value) {
      const updateDto: UpdateWorkflowDefinitionDto = {
        name: form.value.name,
        classificationId: form.value.classificationId || null,
        steps: form.value.steps,
        isActive: form.value.isActive ?? true
      }
      await update(editingWorkflow.value.id, updateDto)
    } else {
      const createDto: CreateWorkflowDefinitionDto = {
        name: form.value.name,
        classificationId: form.value.classificationId || null,
        steps: form.value.steps
      }
      await create(createDto)
    }

    await loadWorkflows()
    closeModal()
  } catch (error: any) {
    console.error('Error saving workflow:', error)
    alert(error.data?.error || error.message || 'Villa kom upp við að vista ferli')
  } finally {
    saving.value = false
  }
}

const closeModal = () => {
  showCreateModal.value = false
  editingWorkflow.value = null
  form.value = {
    name: '',
    classificationId: null,
    steps: [],
    isActive: true
  }
}

onMounted(async () => {
  await Promise.all([loadWorkflows(), loadClassifications(), loadStepHandlers()])
})
</script>

