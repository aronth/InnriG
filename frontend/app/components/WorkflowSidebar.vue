<template>
  <div class="flex-shrink-0 bg-white border-l border-gray-200 flex flex-col overflow-hidden transition-all duration-300"
       :class="isCollapsed ? 'w-12' : 'w-96'">
    <!-- Header -->
    <div class="px-4 py-3 border-b border-gray-200 bg-gray-50 flex-shrink-0 flex items-center"
         :class="isCollapsed ? 'justify-center' : 'justify-between'">
      <h2 v-if="!isCollapsed" class="text-sm font-semibold text-gray-900">Ferlar</h2>
      <button
        @click="isCollapsed = !isCollapsed"
        class="p-1 rounded hover:bg-gray-200 focus:outline-none focus:ring-2 focus:ring-indigo-500 transition-colors"
        :class="isCollapsed ? '' : 'ml-auto'"
        :title="isCollapsed ? 'Sýna ferla' : 'Fela ferla'"
      >
        <svg
          v-if="!isCollapsed"
          class="w-5 h-5 text-gray-600"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
        </svg>
        <svg
          v-else
          class="w-5 h-5 text-gray-600"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
        </svg>
      </button>
    </div>

    <!-- Collapsed Vertical Text -->
    <div v-if="isCollapsed" class="flex-1 flex items-center justify-center">
      <div class="text-sm font-semibold text-gray-600 tracking-wider transform -rotate-90 whitespace-nowrap">
        Ferlar
      </div>
    </div>

    <!-- Content -->
    <div v-if="!isCollapsed" ref="contentRef" class="flex-1 overflow-y-auto p-4">
      <!-- Loading -->
      <div v-if="loading" class="flex items-center justify-center py-8">
        <div class="inline-block animate-spin rounded-full h-6 w-6 border-b-2 border-indigo-600"></div>
      </div>

      <!-- No Workflow -->
      <div v-else-if="!workflow" class="text-center py-8 text-gray-500">
        <p class="text-sm">Engir ferlar fyrir þetta samtal</p>
      </div>

      <!-- Workflow Content -->
      <div v-else class="space-y-4">
        <!-- Workflow Status -->
        <WorkflowStatus 
          :workflow="workflow"
          :workflow-definition="workflowDefinition"
          @reply-click="handleReplyClick"
        />

        <!-- Approval Interface -->
        <WorkflowApproval
          v-if="workflow.state === 'AwaitingApproval'"
          :workflow="workflow"
          @approved="handleApproved"
          @rejected="handleRejected"
        />

        <!-- Workflow Data Summary -->
        <div v-if="workflow.workflowData && Object.keys(workflow.workflowData).length > 0" class="bg-gray-50 rounded-lg p-4">
          <h4 class="text-xs font-semibold text-gray-700 mb-2">Upplýsingar</h4>
          <div class="space-y-2 text-xs text-gray-600">
            <div
              v-for="(value, key) in filteredWorkflowData"
              :key="key"
              class="flex items-start justify-between gap-2"
            >
              <div class="flex-1 min-w-0">
                <span class="font-medium">{{ formatFieldName(key) }}:</span>
                <span class="ml-1 break-words">{{ formatFieldValue(key, value) }}</span>
              </div>
              <a
                v-if="getFieldActionUrl(key, value)"
                :href="getFieldActionUrl(key, value)"
                target="_blank"
                rel="noopener noreferrer"
                :class="getFieldActionClass(key)"
                :title="getFieldActionTitle(key)"
                class="flex-shrink-0 ml-2 px-2 py-1 text-xs text-white rounded hover:opacity-90 focus:outline-none focus:ring-2 focus:ring-indigo-500 transition-colors flex items-center gap-1"
              >
                <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
                </svg>
                {{ getFieldActionLabel(key) }}
              </a>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { WorkflowInstanceDto } from '~/types/workflow'
import type { WorkflowDefinition } from '~/types/workflowDefinition'

const props = defineProps<{
  conversationId: string
}>()

const emit = defineEmits<{
  'reply-click': [draftResponse: string]
}>()

const handleReplyClick = (draftResponse: string) => {
  emit('reply-click', draftResponse)
}

const { getWorkflowByConversation, getWorkflowDefinitionByName } = useWorkflows()

const workflow = ref<WorkflowInstanceDto | null>(null)
const workflowDefinition = ref<WorkflowDefinition | null>(null)
const loading = ref(true)
const isCollapsed = ref(true) // Default to collapsed
const contentRef = ref<HTMLElement | null>(null)
const scrollPosition = ref(0)

const loadWorkflow = async (isInitialLoad = false) => {
  try {
    // Only show loading spinner on initial load, not on refreshes
    if (isInitialLoad) {
      loading.value = true
    }
    
    // Save scroll position before refresh
    if (contentRef.value) {
      scrollPosition.value = contentRef.value.scrollTop
    }
    
    workflow.value = await getWorkflowByConversation(props.conversationId)
    
    // Load workflow definition if workflow exists
    if (workflow.value) {
      try {
        workflowDefinition.value = await getWorkflowDefinitionByName(workflow.value.workflowType)
      } catch (error) {
        console.error('Error loading workflow definition:', error)
        workflowDefinition.value = null
      }
      isCollapsed.value = false
    } else {
      workflowDefinition.value = null
      isCollapsed.value = true
    }
    
    // Restore scroll position after data loads
    await nextTick()
    if (contentRef.value && scrollPosition.value > 0) {
      contentRef.value.scrollTop = scrollPosition.value
    }
  } catch (error) {
    console.error('Error loading workflow:', error)
    workflow.value = null
    workflowDefinition.value = null
    isCollapsed.value = true // Collapse if no workflow
  } finally {
    loading.value = false
  }
}

const handleApproved = () => {
  // Reload workflow to get updated state (not initial load, so no loading spinner)
  loadWorkflow(false)
}

const handleRejected = () => {
  // Reload workflow to get updated state (not initial load, so no loading spinner)
  loadWorkflow(false)
}

// Filter out internal/system fields that shouldn't be displayed
const filteredWorkflowData = computed(() => {
  if (!workflow.value?.workflowData) return {}
  
  const filtered: Record<string, any> = {}
  // Exclude fields that are shown elsewhere or are internal
  const excludeFields = [
    'ApprovalPrompt', 
    'MatchedOrders',
    'ProposedCreditAmount', // Shown in approval section, not needed in summary
    'DraftResponse' // Shown in approval section, not needed in summary
  ]
  
  for (const [key, value] of Object.entries(workflow.value.workflowData)) {
    if (!excludeFields.includes(key) && value !== null && value !== undefined && value !== '') {
      filtered[key] = value
    }
  }
  
  return filtered
})

const formatFieldName = (key: string): string => {
  // Convert camelCase/PascalCase to readable format
  return key
    .replace(/([A-Z])/g, ' $1')
    .replace(/^./, str => str.toUpperCase())
    .trim()
}

const formatFieldValue = (key: string, value: any): string => {
  // Special formatting for known field types
  if (key.includes('Amount') || key.includes('Price') || key.includes('Credit')) {
    return formatCurrency(Number(value))
  }
  if (key.includes('Date') && typeof value === 'string') {
    return formatDate(value)
  }
  if (key.includes('Confidence') && typeof value === 'number') {
    return `${(value * 100).toFixed(0)}%`
  }
  if (typeof value === 'object' && value !== null) {
    return JSON.stringify(value)
  }
  return String(value)
}

const formatCurrency = (amount: number) => {
  return new Intl.NumberFormat('is-IS', {
    style: 'currency',
    currency: 'ISK',
    minimumFractionDigits: 0
  }).format(amount)
}

const formatDate = (dateString: string) => {
  try {
    const date = new Date(dateString)
    return new Intl.DateTimeFormat('is-IS', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit'
    }).format(date)
  } catch {
    return dateString
  }
}

// Get action URL for specific fields (backward compatibility for known fields)
const getFieldActionUrl = (key: string, value: any): string | null => {
  if (key === 'SelectedOrderId' && value) {
    return `https://www.greifinn.is/is/moya/pilot/order/print/${value}`
  }
  if (key === 'ProposedCreditAmount' && value) {
    return 'https://www.greifinn.is/is/moya/pilot/credit/add'
  }
  if (key === 'BookingDate' && value) {
    return 'https://www.greifinn.is/is/bordapontun/booking/add'
  }
  return null
}

const getFieldActionClass = (key: string): string => {
  if (key === 'SelectedOrderId') {
    return 'bg-indigo-600 hover:bg-indigo-700 focus:ring-indigo-500'
  }
  if (key === 'ProposedCreditAmount') {
    return 'bg-green-600 hover:bg-green-700 focus:ring-green-500'
  }
  if (key === 'BookingDate') {
    return 'bg-blue-600 hover:bg-blue-700 focus:ring-blue-500'
  }
  return 'bg-gray-600 hover:bg-gray-700 focus:ring-gray-500'
}

const getFieldActionLabel = (key: string): string => {
  if (key === 'SelectedOrderId') {
    return 'Opna'
  }
  if (key === 'ProposedCreditAmount') {
    return 'Inneign'
  }
  if (key === 'BookingDate') {
    return 'Opna bókun'
  }
  return 'Opna'
}

const getFieldActionTitle = (key: string): string => {
  if (key === 'SelectedOrderId') {
    return 'Opna pöntun í nýjum flipa'
  }
  if (key === 'ProposedCreditAmount') {
    return 'Opna inneignarsíðu í nýjum flipa'
  }
  if (key === 'BookingDate') {
    return 'Opna bókunarsíðu í nýjum flipa'
  }
  return 'Opna í nýjum flipa'
}

watch(() => props.conversationId, () => {
  if (props.conversationId) {
    // Reset to collapsed when conversation changes
    isCollapsed.value = true
    loadWorkflow(true) // Initial load, show spinner
  } else {
    workflow.value = null
    isCollapsed.value = true
  }
}, { immediate: true })

// Poll for updates when workflow is awaiting approval
let pollInterval: NodeJS.Timeout | null = null
watch(() => workflow.value?.state, (newState) => {
  if (pollInterval) {
    clearInterval(pollInterval)
    pollInterval = null
  }
  
  if (newState === 'AwaitingApproval' || newState === 'InProgress') {
    pollInterval = setInterval(() => {
      loadWorkflow(false) // Refresh, no spinner
    }, 5000) // Poll every 5 seconds
  }
})

onUnmounted(() => {
  if (pollInterval) {
    clearInterval(pollInterval)
  }
})
</script>

