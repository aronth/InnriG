using InnriGreifi.API.Models;
using InnriGreifi.API.Models.DTOs;

namespace InnriGreifi.API.Services;

public interface IWorkflowDefinitionService
{
    Task<List<WorkflowDefinitionDto>> GetAllAsync(CancellationToken ct = default);
    Task<WorkflowDefinitionDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<WorkflowDefinitionDto?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<WorkflowDefinitionDto?> GetByClassificationIdAsync(Guid classificationId, CancellationToken ct = default);
    Task<WorkflowDefinitionDto> CreateAsync(CreateWorkflowDefinitionDto dto, CancellationToken ct = default);
    Task<WorkflowDefinitionDto> UpdateAsync(Guid id, UpdateWorkflowDefinitionDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<List<StepHandlerInfoDto>> GetAvailableStepHandlersAsync();
}

