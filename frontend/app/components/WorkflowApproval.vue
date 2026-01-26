<template>
  <div class="space-y-4">
    <div class="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
      <h3 class="text-sm font-semibold text-yellow-900 mb-2">Bíður samþykki</h3>
      <p class="text-sm text-yellow-800">{{ approvalPrompt }}</p>
    </div>

    <!-- Order Details -->
    <div v-if="selectedOrder" class="bg-white border border-gray-200 rounded-lg p-4">
      <h4 class="text-sm font-semibold text-gray-900 mb-2">Valin pöntun</h4>
      <div class="space-y-1 text-sm">
        <div><span class="font-medium">Pöntunarnúmer:</span> {{ selectedOrder.orderId }}</div>
        <div v-if="selectedOrder.customerName">
          <span class="font-medium">Viðskiptavinur:</span> {{ selectedOrder.customerName }}
        </div>
        <div v-if="selectedOrder.totalPrice">
          <span class="font-medium">Heildarupphæð:</span> {{ formatCurrency(selectedOrder.totalPrice) }}
        </div>
      </div>
    </div>

    <!-- Credit Amount -->
    <div class="bg-white border border-gray-200 rounded-lg p-4">
      <h4 class="text-sm font-semibold text-gray-900 mb-2">Inneignarupphæð</h4>
      <input
        v-model.number="creditAmount"
        type="number"
        step="1"
        min="0"
        class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
        placeholder="Inneignarupphæð"
      />
    </div>

    <!-- Draft Response -->
    <div class="bg-white border border-gray-200 rounded-lg p-4">
      <h4 class="text-sm font-semibold text-gray-900 mb-2">Drög að svari</h4>
      <textarea
        v-model="draftResponse"
        rows="6"
        class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
        placeholder="Svar..."
      ></textarea>
    </div>

    <!-- Actions -->
    <div class="flex gap-2">
      <button
        @click="handleApprove"
        :disabled="approving"
        class="flex-1 px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed"
      >
        {{ approving ? 'Samþykkir...' : 'Samþykkja' }}
      </button>
      <button
        @click="handleReject"
        :disabled="rejecting"
        class="flex-1 px-4 py-2 bg-red-600 text-white rounded-md hover:bg-red-700 disabled:opacity-50 disabled:cursor-not-allowed"
      >
        {{ rejecting ? 'Hafna...' : 'Hafna' }}
      </button>
    </div>

    <!-- Rejection Comment -->
    <div v-if="showRejectComment" class="bg-white border border-gray-200 rounded-lg p-4">
      <label class="block text-sm font-medium text-gray-700 mb-2">Ástæða hafnunar</label>
      <textarea
        v-model="rejectComment"
        rows="3"
        class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-indigo-500"
        placeholder="Ástæða..."
      ></textarea>
      <div class="mt-2 flex gap-2">
        <button
          @click="confirmReject"
          :disabled="rejecting"
          class="px-4 py-2 bg-red-600 text-white rounded-md hover:bg-red-700 disabled:opacity-50"
        >
          Staðfesta hafnun
        </button>
        <button
          @click="showRejectComment = false"
          class="px-4 py-2 bg-gray-200 text-gray-700 rounded-md hover:bg-gray-300"
        >
          Hætta við
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import type { WorkflowInstanceDto } from '~/types/workflow'

const props = defineProps<{
  workflow: WorkflowInstanceDto
}>()

const emit = defineEmits<{
  approved: []
  rejected: []
}>()

const { approveWorkflow, rejectWorkflow } = useWorkflows()

const approvalPrompt = computed(() => {
  return props.workflow.workflowData?.ApprovalPrompt as string || 'Vinsamlegast yfirfarið og samþykkið eftirfarandi:'
})

const selectedOrder = computed(() => {
  const orderId = props.workflow.workflowData?.SelectedOrderId as string
  if (!orderId) return null
  
  // Try to get order details from workflow data
  const matchedOrders = props.workflow.workflowData?.MatchedOrders
  if (matchedOrders && typeof matchedOrders === 'string') {
    try {
      const orders = JSON.parse(matchedOrders)
      return orders.find((o: any) => o.orderId === orderId) || null
    } catch {
      return null
    }
  }
  return null
})

const creditAmount = ref<number>(
  (props.workflow.workflowData?.ProposedCreditAmount as number) || 0
)

const draftResponse = ref<string>(
  (props.workflow.workflowData?.DraftResponse as string) || ''
)

const approving = ref(false)
const rejecting = ref(false)
const showRejectComment = ref(false)
const rejectComment = ref('')

const handleApprove = async () => {
  try {
    approving.value = true
    await approveWorkflow(props.workflow.id, {
      approvalData: {
        ProposedCreditAmount: creditAmount.value,
        DraftResponse: draftResponse.value
      }
    })
    emit('approved')
  } catch (error) {
    console.error('Error approving workflow:', error)
    alert('Villa kom upp við samþykki')
  } finally {
    approving.value = false
  }
}

const handleReject = () => {
  showRejectComment.value = true
}

const confirmReject = async () => {
  try {
    rejecting.value = true
    await rejectWorkflow(props.workflow.id, {
      comments: rejectComment.value
    })
    emit('rejected')
  } catch (error) {
    console.error('Error rejecting workflow:', error)
    alert('Villa kom upp við hafnun')
  } finally {
    rejecting.value = false
    showRejectComment.value = false
  }
}

const formatCurrency = (amount: number) => {
  return new Intl.NumberFormat('is-IS', {
    style: 'currency',
    currency: 'ISK',
    minimumFractionDigits: 0
  }).format(amount)
}
</script>

