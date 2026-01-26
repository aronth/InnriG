export interface WorkflowInstanceDto {
  id: string
  conversationId: string
  workflowType: string
  state: string
  currentStepIndex: number
  workflowData?: Record<string, any>
  stepExecutions: WorkflowStepExecutionDto[]
  approvals: WorkflowApprovalDto[]
  createdAt: string
  updatedAt: string
}

export interface WorkflowStepExecutionDto {
  id: string
  workflowInstanceId: string
  stepType: string
  status: string
  result?: Record<string, any>
  errorMessage?: string
  executedAt: string
}

export interface WorkflowApprovalDto {
  id: string
  workflowInstanceId: string
  stepExecutionId?: string
  approvedByUserId?: string
  approvedByName?: string
  status: string
  comments?: string
  approvalData?: Record<string, any>
  approvedAt?: string
  createdAt: string
}

export interface ApproveWorkflowRequest {
  approvalData?: Record<string, any>
  comments?: string
}

