using System.Text.Json;
using InnriGreifi.API.Data;
using InnriGreifi.API.Models;
using InnriGreifi.API.Models.DTOs;
using InnriGreifi.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InnriGreifi.API.Controllers;

[ApiController]
[Route("api/workflows")]
[Authorize(Roles = "User,Manager,Admin")]
public class WorkflowsController : ControllerBase
{
    private readonly IWorkflowExecutionService _workflowService;
    private readonly AppDbContext _context;
    private readonly ILogger<WorkflowsController> _logger;

    public WorkflowsController(
        IWorkflowExecutionService workflowService,
        AppDbContext context,
        ILogger<WorkflowsController> logger)
    {
        _workflowService = workflowService;
        _context = context;
        _logger = logger;
    }

    [HttpGet("conversations/{conversationId}")]
    public async Task<IActionResult> GetWorkflowByConversation(Guid conversationId, CancellationToken ct = default)
    {
        try
        {
            var workflow = await _workflowService.GetWorkflowByConversationAsync(conversationId, ct);
            if (workflow == null)
            {
                return NotFound();
            }

            var dto = MapToDto(workflow);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow for conversation {ConversationId}", conversationId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{workflowInstanceId}")]
    public async Task<IActionResult> GetWorkflow(Guid workflowInstanceId, CancellationToken ct = default)
    {
        try
        {
            var workflow = await _context.WorkflowInstances
                .Include(w => w.StepExecutions)
                .Include(w => w.Approvals)
                .ThenInclude(a => a.ApprovedBy)
                .FirstOrDefaultAsync(w => w.Id == workflowInstanceId, ct);

            if (workflow == null)
            {
                return NotFound();
            }

            var dto = MapToDto(workflow);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflow {WorkflowInstanceId}", workflowInstanceId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("{workflowInstanceId}/approve")]
    public async Task<IActionResult> ApproveWorkflow(
        Guid workflowInstanceId,
        [FromBody] ApproveWorkflowRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var success = await _workflowService.ApproveWorkflowStepAsync(
                workflowInstanceId,
                userId.Value,
                request.ApprovalData,
                ct);

            if (!success)
            {
                return BadRequest(new { error = "Workflow approval failed" });
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving workflow {WorkflowInstanceId}", workflowInstanceId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("{workflowInstanceId}/reject")]
    public async Task<IActionResult> RejectWorkflow(
        Guid workflowInstanceId,
        [FromBody] ApproveWorkflowRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var success = await _workflowService.RejectWorkflowStepAsync(
                workflowInstanceId,
                userId.Value,
                request.Comments,
                ct);

            if (!success)
            {
                return BadRequest(new { error = "Workflow rejection failed" });
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting workflow {WorkflowInstanceId}", workflowInstanceId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("pending-approvals")]
    public async Task<IActionResult> GetPendingApprovals(CancellationToken ct = default)
    {
        try
        {
            var workflows = await _context.WorkflowInstances
                .Include(w => w.Conversation)
                .Include(w => w.Approvals)
                .Where(w => w.State == "AwaitingApproval")
                .OrderByDescending(w => w.UpdatedAt)
                .ToListAsync(ct);

            var dtos = workflows.Select(MapToDto).ToList();
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending approvals");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private WorkflowInstanceDto MapToDto(WorkflowInstance workflow)
    {
        var dto = new WorkflowInstanceDto
        {
            Id = workflow.Id,
            ConversationId = workflow.ConversationId,
            WorkflowType = workflow.WorkflowType,
            State = workflow.State,
            CurrentStepIndex = workflow.CurrentStepIndex,
            CreatedAt = workflow.CreatedAt,
            UpdatedAt = workflow.UpdatedAt,
            StepExecutions = workflow.StepExecutions.Select(se => new WorkflowStepExecutionDto
            {
                Id = se.Id,
                WorkflowInstanceId = se.WorkflowInstanceId,
                StepType = se.StepType,
                Status = se.Status,
                Result = string.IsNullOrEmpty(se.ResultJson)
                    ? null
                    : JsonSerializer.Deserialize<Dictionary<string, object>>(se.ResultJson),
                ErrorMessage = se.ErrorMessage,
                ExecutedAt = se.ExecutedAt
            }).ToList(),
            Approvals = workflow.Approvals.Select(a => new WorkflowApprovalDto
            {
                Id = a.Id,
                WorkflowInstanceId = a.WorkflowInstanceId,
                StepExecutionId = a.StepExecutionId,
                ApprovedByUserId = a.ApprovedByUserId,
                ApprovedByName = a.ApprovedBy?.Name ?? a.ApprovedBy?.UserName,
                Status = a.Status,
                Comments = a.Comments,
                ApprovalData = string.IsNullOrEmpty(a.ApprovalDataJson)
                    ? null
                    : JsonSerializer.Deserialize<Dictionary<string, object>>(a.ApprovalDataJson),
                ApprovedAt = a.ApprovedAt,
                CreatedAt = a.CreatedAt
            }).ToList()
        };

        // Parse workflow data
        if (!string.IsNullOrEmpty(workflow.WorkflowDataJson))
        {
            try
            {
                dto.WorkflowData = JsonSerializer.Deserialize<Dictionary<string, object>>(workflow.WorkflowDataJson);
            }
            catch
            {
                dto.WorkflowData = new Dictionary<string, object>();
            }
        }

        return dto;
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }
}

