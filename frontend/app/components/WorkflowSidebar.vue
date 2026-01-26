<template>
  <div class="flex-shrink-0 bg-white rounded-lg shadow-md flex flex-col overflow-hidden transition-all duration-300"
       :class="isCollapsed ? 'w-12' : 'w-96'">
    <!-- Header -->
    <div class="px-4 py-3 border-b border-gray-200 bg-gray-50 flex-shrink-0 flex items-center"
         :class="isCollapsed ? 'justify-center' : 'justify-between'">
      <h2 v-if="!isCollapsed" class="text-sm font-semibold text-gray-900">Vinnuflæði</h2>
      <button
        @click="isCollapsed = !isCollapsed"
        class="p-1 rounded hover:bg-gray-200 focus:outline-none focus:ring-2 focus:ring-indigo-500 transition-colors"
        :class="isCollapsed ? '' : 'ml-auto'"
        :title="isCollapsed ? 'Sýna vinnuflæði' : 'Fela vinnuflæði'"
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

    <!-- Content -->
    <div v-if="!isCollapsed" ref="contentRef" class="flex-1 overflow-y-auto p-4">
      <!-- Loading -->
      <div v-if="loading" class="flex items-center justify-center py-8">
        <div class="inline-block animate-spin rounded-full h-6 w-6 border-b-2 border-indigo-600"></div>
      </div>

      <!-- No Workflow -->
      <div v-else-if="!workflow" class="text-center py-8 text-gray-500">
        <p class="text-sm">Engin vinnuflæði fyrir þetta samtal</p>
      </div>

      <!-- Workflow Content -->
      <div v-else class="space-y-4">
        <!-- Workflow Status -->
        <WorkflowStatus 
          :workflow="workflow" 
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
            <div v-if="workflow.workflowData.SelectedOrderId" class="flex items-center justify-between">
              <div>
                <span class="font-medium">Pöntun:</span> {{ workflow.workflowData.SelectedOrderId }}
              </div>
              <a
                :href="getOrderUrl(workflow.workflowData.SelectedOrderId)"
                target="_blank"
                rel="noopener noreferrer"
                class="ml-2 px-2 py-1 text-xs bg-indigo-600 text-white rounded hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 transition-colors flex items-center gap-1"
                title="Opna pöntun í nýjum flipa"
              >
                <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
                </svg>
                Opna
              </a>
            </div>
            <div v-if="workflow.workflowData.ProposedCreditAmount" class="flex items-center justify-between">
              <div>
                <span class="font-medium">Inneign:</span> {{ formatCurrency(workflow.workflowData.ProposedCreditAmount) }}
              </div>
              <a
                href="https://www.greifinn.is/is/moya/pilot/credit/add"
                target="_blank"
                rel="noopener noreferrer"
                class="ml-2 px-2 py-1 text-xs bg-green-600 text-white rounded hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-green-500 transition-colors flex items-center gap-1"
                title="Opna inneignarsíðu í nýjum flipa"
              >
                <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
                </svg>
                Inneign
              </a>
            </div>
            <div v-if="workflow.workflowData.MatchConfidence">
              <span class="font-medium">Áreiðanleiki:</span> {{ (workflow.workflowData.MatchConfidence * 100).toFixed(0) }}%
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { WorkflowInstanceDto } from '~/types/workflow'

const props = defineProps<{
  conversationId: string
}>()

const emit = defineEmits<{
  'reply-click': [draftResponse: string]
}>()

const handleReplyClick = (draftResponse: string) => {
  emit('reply-click', draftResponse)
}

const { getWorkflowByConversation } = useWorkflows()

const workflow = ref<WorkflowInstanceDto | null>(null)
const loading = ref(true)
const isCollapsed = ref(false)
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
    
    // Restore scroll position after data loads
    await nextTick()
    if (contentRef.value && scrollPosition.value > 0) {
      contentRef.value.scrollTop = scrollPosition.value
    }
  } catch (error) {
    console.error('Error loading workflow:', error)
    workflow.value = null
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

const formatCurrency = (amount: number) => {
  return new Intl.NumberFormat('is-IS', {
    style: 'currency',
    currency: 'ISK',
    minimumFractionDigits: 0
  }).format(amount)
}

const getOrderUrl = (orderId: string | number) => {
  return `https://www.greifinn.is/is/moya/pilot/order/print/${orderId}`
}

watch(() => props.conversationId, () => {
  if (props.conversationId) {
    loadWorkflow(true) // Initial load, show spinner
  } else {
    workflow.value = null
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

