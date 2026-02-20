using InnriGreifi.API.Models.DTOs;
using InnriGreifi.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InnriGreifi.API.Controllers;

[ApiController]
[Route("api/workflow-definitions")]
// [Authorize(Roles = "SystemAdmin,Admin")] // Temporarily disabled
public class WorkflowDefinitionsController : ControllerBase
{
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly ILogger<WorkflowDefinitionsController> _logger;

    public WorkflowDefinitionsController(
        IWorkflowDefinitionService workflowDefinitionService,
        ILogger<WorkflowDefinitionsController> logger)
    {
        _workflowDefinitionService = workflowDefinitionService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<WorkflowDefinitionDto>>> GetAll()
    {
        var workflows = await _workflowDefinitionService.GetAllAsync();
        return Ok(workflows);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<WorkflowDefinitionDto>> GetById(Guid id)
    {
        var workflow = await _workflowDefinitionService.GetByIdAsync(id);
        if (workflow == null)
        {
            return NotFound();
        }
        return Ok(workflow);
    }

    [HttpPost]
    public async Task<ActionResult<WorkflowDefinitionDto>> Create([FromBody] CreateWorkflowDefinitionDto dto)
    {
        try
        {
            var workflow = await _workflowDefinitionService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = workflow.Id }, workflow);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<WorkflowDefinitionDto>> Update(Guid id, [FromBody] UpdateWorkflowDefinitionDto dto)
    {
        try
        {
            var workflow = await _workflowDefinitionService.UpdateAsync(id, dto);
            return Ok(workflow);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _workflowDefinitionService.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpGet("step-handlers")]
    public async Task<ActionResult<List<StepHandlerInfoDto>>> GetStepHandlers()
    {
        var handlers = await _workflowDefinitionService.GetAvailableStepHandlersAsync();
        return Ok(handlers);
    }
}

