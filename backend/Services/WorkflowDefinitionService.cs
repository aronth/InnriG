using System.Text.Json;
using InnriGreifi.API.Data;
using InnriGreifi.API.Models;
using InnriGreifi.API.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace InnriGreifi.API.Services;

public class WorkflowDefinitionService : IWorkflowDefinitionService
{
    private readonly AppDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkflowDefinitionService> _logger;

    public WorkflowDefinitionService(
        AppDbContext context,
        IServiceProvider serviceProvider,
        ILogger<WorkflowDefinitionService> logger)
    {
        _context = context;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<List<WorkflowDefinitionDto>> GetAllAsync(CancellationToken ct = default)
    {
        var workflows = await _context.WorkflowDefinitions
            .Include(w => w.Classification)
            .OrderBy(w => w.Name)
            .ToListAsync(ct);

        return workflows.Select(MapToDto).ToList();
    }

    public async Task<WorkflowDefinitionDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var workflow = await _context.WorkflowDefinitions
            .Include(w => w.Classification)
            .FirstOrDefaultAsync(w => w.Id == id, ct);

        return workflow == null ? null : MapToDto(workflow);
    }

    public async Task<WorkflowDefinitionDto?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var workflow = await _context.WorkflowDefinitions
            .Include(w => w.Classification)
            .FirstOrDefaultAsync(w => w.Name == name && w.IsActive, ct);

        return workflow == null ? null : MapToDto(workflow);
    }

    public async Task<WorkflowDefinitionDto?> GetByClassificationIdAsync(Guid classificationId, CancellationToken ct = default)
    {
        var workflow = await _context.WorkflowDefinitions
            .Include(w => w.Classification)
            .FirstOrDefaultAsync(w => w.ClassificationId == classificationId && w.IsActive, ct);

        return workflow == null ? null : MapToDto(workflow);
    }

    public async Task<WorkflowDefinitionDto> CreateAsync(CreateWorkflowDefinitionDto dto, CancellationToken ct = default)
    {
        // Validate classification exists if provided
        if (dto.ClassificationId.HasValue)
        {
            var classificationExists = await _context.EmailClassifications
                .AnyAsync(c => c.Id == dto.ClassificationId.Value, ct);
            
            if (!classificationExists)
            {
                throw new InvalidOperationException($"Classification with ID {dto.ClassificationId} does not exist");
            }
        }

        // Validate name is unique
        var nameExists = await _context.WorkflowDefinitions
            .AnyAsync(w => w.Name == dto.Name, ct);
        
        if (nameExists)
        {
            throw new InvalidOperationException($"Workflow with name '{dto.Name}' already exists");
        }

        // Validate step handlers exist
        foreach (var step in dto.Steps)
        {
            if (!IsHandlerTypeValid(step.HandlerType))
            {
                throw new InvalidOperationException($"Handler type '{step.HandlerType}' is not registered");
            }
        }

        var workflow = new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            ClassificationId = dto.ClassificationId,
            Steps = dto.Steps.Select(s => new WorkflowStepDefinition
            {
                StepType = s.StepType,
                HandlerType = s.HandlerType,
                Order = s.Order,
                RequiresApproval = s.RequiresApproval,
                Configuration = s.Configuration
            }).ToList(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.WorkflowDefinitions.Add(workflow);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Created workflow definition {WorkflowName} with ID {WorkflowId}", workflow.Name, workflow.Id);

        return await GetByIdAsync(workflow.Id, ct) ?? throw new InvalidOperationException("Failed to retrieve created workflow");
    }

    public async Task<WorkflowDefinitionDto> UpdateAsync(Guid id, UpdateWorkflowDefinitionDto dto, CancellationToken ct = default)
    {
        var workflow = await _context.WorkflowDefinitions
            .FirstOrDefaultAsync(w => w.Id == id, ct);

        if (workflow == null)
        {
            throw new InvalidOperationException($"Workflow with ID {id} does not exist");
        }

        // Validate classification exists if provided
        if (dto.ClassificationId.HasValue)
        {
            var classificationExists = await _context.EmailClassifications
                .AnyAsync(c => c.Id == dto.ClassificationId.Value, ct);
            
            if (!classificationExists)
            {
                throw new InvalidOperationException($"Classification with ID {dto.ClassificationId} does not exist");
            }
        }

        // Validate name is unique (if changed)
        if (workflow.Name != dto.Name)
        {
            var nameExists = await _context.WorkflowDefinitions
                .AnyAsync(w => w.Name == dto.Name && w.Id != id, ct);
            
            if (nameExists)
            {
                throw new InvalidOperationException($"Workflow with name '{dto.Name}' already exists");
            }
        }

        // Validate step handlers exist
        foreach (var step in dto.Steps)
        {
            if (!IsHandlerTypeValid(step.HandlerType))
            {
                throw new InvalidOperationException($"Handler type '{step.HandlerType}' is not registered");
            }
        }

        workflow.Name = dto.Name;
        workflow.ClassificationId = dto.ClassificationId;
        workflow.Steps = dto.Steps.Select(s => new WorkflowStepDefinition
        {
            StepType = s.StepType,
            HandlerType = s.HandlerType,
            Order = s.Order,
            RequiresApproval = s.RequiresApproval,
            Configuration = s.Configuration
        }).ToList();
        workflow.IsActive = dto.IsActive;
        workflow.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Updated workflow definition {WorkflowName} with ID {WorkflowId}", workflow.Name, workflow.Id);

        return await GetByIdAsync(workflow.Id, ct) ?? throw new InvalidOperationException("Failed to retrieve updated workflow");
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var workflow = await _context.WorkflowDefinitions
            .FirstOrDefaultAsync(w => w.Id == id, ct);

        if (workflow == null)
        {
            return false;
        }

        // Check if workflow has instances
        var hasInstances = await _context.WorkflowInstances
            .AnyAsync(wi => wi.WorkflowType == workflow.Name, ct);

        if (hasInstances)
        {
            // Soft delete
            workflow.IsActive = false;
            workflow.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Soft deleted workflow definition {WorkflowName} with ID {WorkflowId}", workflow.Name, workflow.Id);
        }
        else
        {
            // Hard delete if no instances
            _context.WorkflowDefinitions.Remove(workflow);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Deleted workflow definition {WorkflowName} with ID {WorkflowId}", workflow.Name, workflow.Id);
        }

        return true;
    }

    public Task<List<StepHandlerInfoDto>> GetAvailableStepHandlersAsync()
    {
        var handlers = new List<StepHandlerInfoDto>();

        // Get all registered step handlers from DI container
        var handlerTypes = typeof(WorkflowDefinitionService).Assembly.GetTypes()
            .Where(t => typeof(IWorkflowStepHandler).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            try
            {
                var handler = _serviceProvider.GetService(handlerType) as IWorkflowStepHandler;
                if (handler != null)
                {
                    handlers.Add(new StepHandlerInfoDto
                    {
                        HandlerType = handlerType.FullName ?? handlerType.Name,
                        StepType = handlerType.Name.Replace("StepHandler", ""),
                        Description = $"Handler for {handlerType.Name.Replace("StepHandler", "")} step"
                    });
                }
            }
            catch
            {
                // Skip if handler can't be resolved
            }
        }

        return Task.FromResult(handlers.OrderBy(h => h.StepType).ToList());
    }

    private WorkflowDefinitionDto MapToDto(WorkflowDefinition workflow)
    {
        return new WorkflowDefinitionDto
        {
            Id = workflow.Id,
            Name = workflow.Name,
            ClassificationId = workflow.ClassificationId,
            Classification = workflow.Classification != null ? new EmailClassificationDto
            {
                Id = workflow.Classification.Id,
                Name = workflow.Classification.Name,
                Description = workflow.Classification.Description,
                SystemPrompt = workflow.Classification.SystemPrompt,
                IsSystem = workflow.Classification.IsSystem,
                IsActive = workflow.Classification.IsActive,
                CreatedAt = workflow.Classification.CreatedAt,
                UpdatedAt = workflow.Classification.UpdatedAt
            } : null,
            Steps = workflow.Steps.Select(s => new WorkflowStepDefinitionDto
            {
                StepType = s.StepType,
                HandlerType = s.HandlerType,
                Order = s.Order,
                RequiresApproval = s.RequiresApproval,
                Configuration = s.Configuration
            }).ToList(),
            IsActive = workflow.IsActive,
            CreatedAt = workflow.CreatedAt,
            UpdatedAt = workflow.UpdatedAt
        };
    }

    private bool IsHandlerTypeValid(string handlerTypeName)
    {
        try
        {
            var type = Type.GetType(handlerTypeName);
            if (type == null)
            {
                // Try to find in current assembly
                type = typeof(WorkflowDefinitionService).Assembly.GetTypes()
                    .FirstOrDefault(t => t.Name == handlerTypeName && typeof(IWorkflowStepHandler).IsAssignableFrom(t));
            }

            if (type == null)
            {
                return false;
            }

            // Try to resolve from DI
            var handler = _serviceProvider.GetService(type) as IWorkflowStepHandler;
            return handler != null;
        }
        catch
        {
            return false;
        }
    }
}

