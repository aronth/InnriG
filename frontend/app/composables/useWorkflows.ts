import type { WorkflowInstanceDto, WorkflowApprovalDto, ApproveWorkflowRequest } from '~/types/workflow'
import type { WorkflowDefinition } from '~/types/workflowDefinition'

export const useWorkflows = () => {
  const config = useRuntimeConfig()
  const apiBase = config.public.apiBase
  const { apiFetch } = useApi()

  const getWorkflowByConversation = async (conversationId: string): Promise<WorkflowInstanceDto | null> => {
    try {
      return await apiFetch<WorkflowInstanceDto>(`${apiBase}/api/workflows/conversations/${conversationId}`)
    } catch (error: any) {
      if (error.status === 404) {
        return null
      }
      throw error
    }
  }

  const getWorkflow = async (workflowInstanceId: string): Promise<WorkflowInstanceDto> => {
    return await apiFetch<WorkflowInstanceDto>(`${apiBase}/api/workflows/${workflowInstanceId}`)
  }

  const approveWorkflow = async (
    workflowInstanceId: string,
    request: ApproveWorkflowRequest
  ): Promise<void> => {
    await apiFetch(`${apiBase}/api/workflows/${workflowInstanceId}/approve`, {
      method: 'POST',
      body: request
    })
  }

  const rejectWorkflow = async (
    workflowInstanceId: string,
    request: ApproveWorkflowRequest
  ): Promise<void> => {
    await apiFetch(`${apiBase}/api/workflows/${workflowInstanceId}/reject`, {
      method: 'POST',
      body: request
    })
  }

  const getPendingApprovals = async (): Promise<WorkflowInstanceDto[]> => {
    return await apiFetch<WorkflowInstanceDto[]>(`${apiBase}/api/workflows/pending-approvals`)
  }

  const getWorkflowDefinitionByName = async (workflowType: string): Promise<WorkflowDefinition | null> => {
    try {
      return await apiFetch<WorkflowDefinition>(`${apiBase}/api/workflows/definitions/by-name/${workflowType}`)
    } catch (error: any) {
      if (error.status === 404) {
        return null
      }
      throw error
    }
  }

  return {
    getWorkflowByConversation,
    getWorkflow,
    approveWorkflow,
    rejectWorkflow,
    getPendingApprovals,
    getWorkflowDefinitionByName
  }
}

