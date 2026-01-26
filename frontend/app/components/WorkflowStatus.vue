<template>
  <div class="space-y-4">
    <div class="flex items-center justify-between">
      <h3 class="text-sm font-semibold text-gray-900">Vinnuflæði</h3>
      <span
        class="px-2 py-1 text-xs font-semibold rounded-full"
        :class="getStateColor(workflow.state)"
      >
        {{ getStateLabel(workflow.state) }}
      </span>
    </div>

    <!-- Progress Steps -->
    <div class="space-y-2">
      <div
        v-for="(step, index) in workflowSteps"
        :key="index"
        class="flex items-start gap-3"
      >
        <div class="flex-shrink-0 mt-0.5">
          <div
            v-if="step.status === 'Completed'"
            class="w-6 h-6 rounded-full bg-green-500 flex items-center justify-center"
          >
            <svg class="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
            </svg>
          </div>
          <div
            v-else-if="step.status === 'Running'"
            class="w-6 h-6 rounded-full bg-blue-500 flex items-center justify-center"
          >
            <div class="w-3 h-3 rounded-full bg-white animate-pulse"></div>
          </div>
          <div
            v-else-if="step.status === 'Failed'"
            class="w-6 h-6 rounded-full bg-red-500 flex items-center justify-center"
          >
            <svg class="w-4 h-4 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </div>
          <div
            v-else
            class="w-6 h-6 rounded-full bg-gray-300 border-2 border-gray-400"
          ></div>
        </div>
        <div class="flex-1 min-w-0">
          <div class="flex items-center justify-between">
            <div class="text-sm font-medium text-gray-900">{{ step.name }}</div>
            <button
              v-if="step.stepType === 'EmailSend' && (step.status === 'Running' || step.status === 'Pending') && workflow.workflowData?.DraftResponse"
              @click="handleReplyClick"
              class="ml-2 px-3 py-1 text-xs bg-indigo-600 text-white rounded hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 flex items-center gap-1"
            >
              <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h10a8 8 0 018 8v2M3 10l6 6m-6-6l6-6" />
              </svg>
              Svara
            </button>
          </div>
          <div v-if="step.status === 'Completed' && step.result" class="text-xs text-gray-500 mt-1">
            {{ formatStepResult(step) }}
          </div>
          <div v-if="step.status === 'Failed' && step.errorMessage" class="text-xs text-red-600 mt-1">
            {{ step.errorMessage }}
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { WorkflowInstanceDto, WorkflowStepExecutionDto } from '~/types/workflow'

const props = defineProps<{
  workflow: WorkflowInstanceDto
}>()

const emit = defineEmits<{
  'reply-click': [draftResponse: string]
}>()

const handleReplyClick = () => {
  const draftResponse = props.workflow.workflowData?.DraftResponse
  if (draftResponse) {
    emit('reply-click', typeof draftResponse === 'string' ? draftResponse : String(draftResponse))
  }
}

const workflowSteps = computed(() => {
  const stepTypes = [
    'OrderLookup',
    'OrderVerification',
    'CreditCalculation',
    'ResponseDraft',
    'Approval',
    'CreditIssuance',
    'EmailSend'
  ]

  const stepNames: Record<string, string> = {
    OrderLookup: 'Leita að pöntun',
    OrderVerification: 'Staðfesta pöntun',
    CreditCalculation: 'Reikna inneign',
    ResponseDraft: 'Drög að svari',
    Approval: 'Samþykki',
    CreditIssuance: 'Úthluta inneign',
    EmailSend: 'Senda svar'
  }

  return stepTypes.map((stepType, index) => {
    const execution = props.workflow.stepExecutions.find(se => se.stepType === stepType)
    return {
      name: stepNames[stepType] || stepType,
      stepType,
      status: execution?.status || (index < props.workflow.currentStepIndex ? 'Completed' : 'Pending'),
      result: execution?.result,
      errorMessage: execution?.errorMessage
    }
  })
})

const getStateColor = (state: string) => {
  const colors: Record<string, string> = {
    Pending: 'bg-gray-100 text-gray-800',
    InProgress: 'bg-blue-100 text-blue-800',
    AwaitingApproval: 'bg-yellow-100 text-yellow-800',
    Completed: 'bg-green-100 text-green-800',
    Failed: 'bg-red-100 text-red-800'
  }
  return colors[state] || 'bg-gray-100 text-gray-800'
}

const getStateLabel = (state: string) => {
  const labels: Record<string, string> = {
    Pending: 'Í bið',
    InProgress: 'Í vinnslu',
    AwaitingApproval: 'Bíður samþykki',
    Completed: 'Lokið',
    Failed: 'Mistókst'
  }
  return labels[state] || state
}

const formatStepResult = (step: any) => {
  if (!step.result) return ''
  
  if (step.stepType === 'OrderLookup' && step.result.MatchCount !== undefined) {
    return `Fann ${step.result.MatchCount} pöntun(ar)`
  }
  if (step.stepType === 'CreditCalculation' && step.result.ProposedCreditAmount !== undefined) {
    return `${formatCurrency(step.result.ProposedCreditAmount)}`
  }
  if (step.stepType === 'OrderVerification' && step.result.MatchConfidence !== undefined) {
    return `Áreiðanleiki: ${(step.result.MatchConfidence * 100).toFixed(0)}%`
  }
  return ''
}

const formatCurrency = (amount: number) => {
  return new Intl.NumberFormat('is-IS', {
    style: 'currency',
    currency: 'ISK',
    minimumFractionDigits: 0
  }).format(amount)
}
</script>

