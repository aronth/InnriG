import type { EmailClassification } from './emailClassification'

export interface WorkflowDefinition {
  id: string
  name: string
  classificationId?: string | null
  classification?: EmailClassification | null
  steps: WorkflowStepDefinition[]
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface WorkflowStepDefinition {
  stepType: string
  handlerType: string
  order: number
  requiresApproval: boolean
  configuration: Record<string, any>
}

export interface CreateWorkflowDefinitionDto {
  name: string
  classificationId?: string | null
  steps: WorkflowStepDefinition[]
}

export interface UpdateWorkflowDefinitionDto {
  name: string
  classificationId?: string | null
  steps: WorkflowStepDefinition[]
  isActive: boolean
}

export interface StepHandlerInfo {
  handlerType: string
  stepType: string
  description: string
}

