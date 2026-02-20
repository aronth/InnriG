using System.Text.Json;
using InnriGreifi.API.Data;
using InnriGreifi.API.Models;
using Microsoft.EntityFrameworkCore;


namespace InnriGreifi.API.Services;

public class WorkflowExecutionService : IWorkflowExecutionService
{
    private readonly AppDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkflowExecutionService> _logger;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;

    public WorkflowExecutionService(
        AppDbContext context,
        IServiceProvider serviceProvider,
        ILogger<WorkflowExecutionService> logger,
        IWorkflowDefinitionService workflowDefinitionService)
    {
        _context = context;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _workflowDefinitionService = workflowDefinitionService;
    }

    public async Task<WorkflowInstance?> InitializeWorkflowAsync(Guid conversationId, string classification, CancellationToken ct = default)
    {
        try
        {
            // Check if workflow already exists
            var existing = await _context.WorkflowInstances
                .FirstOrDefaultAsync(w => w.ConversationId == conversationId, ct);

            if (existing != null)
            {
                _logger.LogInformation("Workflow instance already exists for conversation {ConversationId}", conversationId);
                return existing;
            }

            // Look up classification by name in database
            var emailClassification = await _context.EmailClassifications
                .FirstOrDefaultAsync(c => c.Name == classification && c.IsActive, ct);

            if (emailClassification == null)
            {
                _logger.LogDebug("Classification {Classification} not found in database", classification);
                return null;
            }

            // Check if workflow exists for this classification
            var workflowDefDto = await _workflowDefinitionService.GetByClassificationIdAsync(emailClassification.Id, ct);
            if (workflowDefDto == null)
            {
                _logger.LogDebug("No workflow defined for classification {Classification} (ID: {ClassificationId})", classification, emailClassification.Id);
                return null;
            }

            // Create workflow instance
            var instance = new WorkflowInstance
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                WorkflowType = workflowDefDto.Name,
                State = "Pending",
                CurrentStepIndex = 0,
                WorkflowDataJson = "{}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.WorkflowInstances.Add(instance);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Created workflow instance {WorkflowInstanceId} of type {WorkflowType} for conversation {ConversationId}",
                instance.Id, instance.WorkflowType, conversationId);

            // Start execution
            await ExecuteWorkflowAsync(instance.Id, ct);

            return instance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing workflow for conversation {ConversationId}", conversationId);
            throw;
        }
    }

    public async Task ExecuteWorkflowAsync(Guid workflowInstanceId, CancellationToken ct = default)
    {
        try
        {
            var instance = await _context.WorkflowInstances
                .Include(w => w.Conversation)
                    .ThenInclude(c => c.ExtractedData)
                .Include(w => w.Conversation)
                    .ThenInclude(c => c.Messages)
                .FirstOrDefaultAsync(w => w.Id == workflowInstanceId, ct);

            if (instance == null)
            {
                _logger.LogWarning("Workflow instance {WorkflowInstanceId} not found", workflowInstanceId);
                return;
            }

            // Get workflow definition from database by name
            var workflowDefEntity = await _context.WorkflowDefinitions
                .FirstOrDefaultAsync(w => w.Name == instance.WorkflowType && w.IsActive, ct);

            if (workflowDefEntity == null)
            {
                _logger.LogWarning("Workflow definition not found for type {WorkflowType}", instance.WorkflowType);
                instance.State = "Failed";
                instance.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);
                return;
            }

            // Convert entity directly to model (no need for DTO service here)
            var workflowDef = new WorkflowDefinition
            {
                Name = workflowDefEntity.Name,
                Steps = workflowDefEntity.Steps // This will deserialize from StepsJson
            };

            // Load workflow data
            var workflowData = DeserializeWorkflowData(instance.WorkflowDataJson);

            // Update state to InProgress if pending
            if (instance.State == "Pending")
            {
                instance.State = "InProgress";
                instance.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);
            }

            // Execute steps starting from current index (sort by Order to ensure correct execution order)
            var sortedSteps = workflowDef.Steps.OrderBy(s => s.Order).ToList();
            for (int i = instance.CurrentStepIndex; i < sortedSteps.Count; i++)
            {
                var stepDef = sortedSteps[i];
                _logger.LogInformation("Executing step {StepIndex}: {StepType} for workflow {WorkflowInstanceId}",
                    i, stepDef.StepType, workflowInstanceId);

                // Create step execution record
                var stepExecution = new WorkflowStepExecution
                {
                    Id = Guid.NewGuid(),
                    WorkflowInstanceId = instance.Id,
                    StepType = stepDef.StepType,
                    Status = "Running",
                    ExecutedAt = DateTime.UtcNow
                };

                _context.WorkflowStepExecutions.Add(stepExecution);
                await _context.SaveChangesAsync(ct);

                try
                {
                    // Resolve handler
                    var handler = ResolveHandler(stepDef.HandlerType);
                    if (handler == null)
                    {
                        throw new InvalidOperationException($"Handler {stepDef.HandlerType} not found");
                    }

                    // Execute step
                    var result = await handler.ExecuteAsync(
                        instance,
                        instance.Conversation,
                        instance.Conversation.ExtractedData,
                        stepDef.Configuration,
                        ct);

                    // Update step execution
                    stepExecution.Status = result.Success ? "Completed" : "Failed";
                    stepExecution.ResultJson = JsonSerializer.Serialize(result.OutputData);
                    stepExecution.ErrorMessage = result.ErrorMessage;
                    stepExecution.ExecutedAt = DateTime.UtcNow;

                    // Merge output data into workflow data
                    foreach (var kvp in result.OutputData)
                    {
                        workflowData[kvp.Key] = kvp.Value;
                    }

                    // Update workflow instance
                    instance.WorkflowDataJson = SerializeWorkflowData(workflowData);
                    instance.CurrentStepIndex = i + 1;
                    instance.UpdatedAt = DateTime.UtcNow;

                    if (!result.Success)
                    {
                        instance.State = "Failed";
                        await _context.SaveChangesAsync(ct);
                        _logger.LogError("Step {StepType} failed for workflow {WorkflowInstanceId}: {Error}",
                            stepDef.StepType, workflowInstanceId, result.ErrorMessage);
                        return;
                    }

                    // Check if approval required
                    if (result.RequiresApproval || stepDef.RequiresApproval)
                    {
                        instance.State = "AwaitingApproval";
                        await _context.SaveChangesAsync(ct);
                        _logger.LogInformation("Workflow {WorkflowInstanceId} paused at step {StepIndex} awaiting approval",
                            workflowInstanceId, i);
                        return;
                    }

                    await _context.SaveChangesAsync(ct);
                }
                catch (Exception ex)
                {
                    stepExecution.Status = "Failed";
                    stepExecution.ErrorMessage = ex.Message;
                    stepExecution.ExecutedAt = DateTime.UtcNow;
                    instance.State = "Failed";
                    instance.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(ct);
                    _logger.LogError(ex, "Error executing step {StepType} for workflow {WorkflowInstanceId}",
                        stepDef.StepType, workflowInstanceId);
                    return;
                }
            }

            // All steps completed
            instance.State = "Completed";
            instance.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Workflow {WorkflowInstanceId} completed successfully", workflowInstanceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing workflow {WorkflowInstanceId}", workflowInstanceId);
            throw;
        }
    }

    public async Task<bool> ApproveWorkflowStepAsync(Guid workflowInstanceId, Guid userId, Dictionary<string, object>? approvalData = null, CancellationToken ct = default)
    {
        try
        {
            var instance = await _context.WorkflowInstances
                .FirstOrDefaultAsync(w => w.Id == workflowInstanceId, ct);

            if (instance == null)
            {
                _logger.LogWarning("Workflow instance {WorkflowInstanceId} not found", workflowInstanceId);
                return false;
            }

            if (instance.State != "AwaitingApproval")
            {
                _logger.LogWarning("Workflow {WorkflowInstanceId} is not awaiting approval (state: {State})",
                    workflowInstanceId, instance.State);
                return false;
            }

            // Create approval record
            var approval = new WorkflowApproval
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = instance.Id,
                ApprovedByUserId = userId,
                Status = "Approved",
                ApprovedAt = DateTime.UtcNow,
                ApprovalDataJson = approvalData != null ? JsonSerializer.Serialize(approvalData) : null,
                CreatedAt = DateTime.UtcNow
            };

            _context.WorkflowApprovals.Add(approval);

            // Merge approval data into workflow data if provided
            if (approvalData != null)
            {
                var workflowData = DeserializeWorkflowData(instance.WorkflowDataJson);
                foreach (var kvp in approvalData)
                {
                    workflowData[kvp.Key] = kvp.Value;
                }
                instance.WorkflowDataJson = SerializeWorkflowData(workflowData);
            }

            // Continue workflow execution
            instance.State = "InProgress";
            instance.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            // Continue execution
            await ExecuteWorkflowAsync(workflowInstanceId, ct);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving workflow step {WorkflowInstanceId}", workflowInstanceId);
            throw;
        }
    }

    public async Task<bool> RejectWorkflowStepAsync(Guid workflowInstanceId, Guid userId, string? reason = null, CancellationToken ct = default)
    {
        try
        {
            var instance = await _context.WorkflowInstances
                .FirstOrDefaultAsync(w => w.Id == workflowInstanceId, ct);

            if (instance == null)
            {
                _logger.LogWarning("Workflow instance {WorkflowInstanceId} not found", workflowInstanceId);
                return false;
            }

            if (instance.State != "AwaitingApproval")
            {
                _logger.LogWarning("Workflow {WorkflowInstanceId} is not awaiting approval (state: {State})",
                    workflowInstanceId, instance.State);
                return false;
            }

            // Create rejection record
            var approval = new WorkflowApproval
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = instance.Id,
                ApprovedByUserId = userId,
                Status = "Rejected",
                Comments = reason,
                ApprovedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.WorkflowApprovals.Add(approval);

            // Mark workflow as failed
            instance.State = "Failed";
            instance.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Workflow {WorkflowInstanceId} rejected by user {UserId}: {Reason}",
                workflowInstanceId, userId, reason);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting workflow step {WorkflowInstanceId}", workflowInstanceId);
            throw;
        }
    }

    public async Task<WorkflowInstance?> GetWorkflowByConversationAsync(Guid conversationId, CancellationToken ct = default)
    {
        return await _context.WorkflowInstances
            .Include(w => w.StepExecutions)
            .Include(w => w.Approvals)
            .ThenInclude(a => a.ApprovedBy)
            .FirstOrDefaultAsync(w => w.ConversationId == conversationId, ct);
    }

    private IWorkflowStepHandler? ResolveHandler(string handlerTypeName)
    {
        try
        {
            // Try to resolve by full type name first
            var type = Type.GetType(handlerTypeName);
            if (type == null)
            {
                // Try to find in current assembly
                type = typeof(WorkflowExecutionService).Assembly.GetTypes()
                    .FirstOrDefault(t => t.Name == handlerTypeName && typeof(IWorkflowStepHandler).IsAssignableFrom(t));
            }

            if (type == null)
            {
                _logger.LogWarning("Handler type {HandlerType} not found", handlerTypeName);
                return null;
            }

            return (IWorkflowStepHandler)_serviceProvider.GetRequiredService(type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving handler {HandlerType}", handlerTypeName);
            return null;
        }
    }

    private Dictionary<string, object> DeserializeWorkflowData(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return new Dictionary<string, object>();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }

    private string SerializeWorkflowData(Dictionary<string, object> data)
    {
        try
        {
            return JsonSerializer.Serialize(data);
        }
        catch
        {
            return "{}";
        }
    }
}

