import type { WorkflowDefinition, CreateWorkflowDefinitionDto, UpdateWorkflowDefinitionDto, StepHandlerInfo } from '~/types/workflowDefinition'

export const useWorkflowDefinitions = () => {
  const config = useRuntimeConfig()
  const apiBase = config.public.apiBase
  const { apiFetch } = useApi()

  const getAll = async (): Promise<WorkflowDefinition[]> => {
    return await apiFetch<WorkflowDefinition[]>(`${apiBase}/api/workflow-definitions`)
  }

  const getById = async (id: string): Promise<WorkflowDefinition> => {
    return await apiFetch<WorkflowDefinition>(`${apiBase}/api/workflow-definitions/${id}`)
  }

  const create = async (dto: CreateWorkflowDefinitionDto): Promise<WorkflowDefinition> => {
    return await apiFetch<WorkflowDefinition>(`${apiBase}/api/workflow-definitions`, {
      method: 'POST',
      body: dto
    })
  }

  const update = async (id: string, dto: UpdateWorkflowDefinitionDto): Promise<WorkflowDefinition> => {
    return await apiFetch<WorkflowDefinition>(`${apiBase}/api/workflow-definitions/${id}`, {
      method: 'PUT',
      body: dto
    })
  }

  const deleteWorkflow = async (id: string): Promise<void> => {
    await apiFetch<void>(`${apiBase}/api/workflow-definitions/${id}`, {
      method: 'DELETE'
    })
  }

  const getStepHandlers = async (): Promise<StepHandlerInfo[]> => {
    return await apiFetch<StepHandlerInfo[]>(`${apiBase}/api/workflow-definitions/step-handlers`)
  }

  return {
    getAll,
    getById,
    create,
    update,
    delete: deleteWorkflow,
    getStepHandlers
  }
}

