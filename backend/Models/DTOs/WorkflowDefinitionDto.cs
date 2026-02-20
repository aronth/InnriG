namespace InnriGreifi.API.Models.DTOs;

public class WorkflowDefinitionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ClassificationId { get; set; }
    public EmailClassificationDto? Classification { get; set; }
    public List<WorkflowStepDefinitionDto> Steps { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class WorkflowStepDefinitionDto
{
    public string StepType { get; set; } = string.Empty;
    public string HandlerType { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool RequiresApproval { get; set; }
    public Dictionary<string, object> Configuration { get; set; } = new();
}

public class CreateWorkflowDefinitionDto
{
    public string Name { get; set; } = string.Empty;
    public Guid? ClassificationId { get; set; }
    public List<WorkflowStepDefinitionDto> Steps { get; set; } = new();
}

public class UpdateWorkflowDefinitionDto
{
    public string Name { get; set; } = string.Empty;
    public Guid? ClassificationId { get; set; }
    public List<WorkflowStepDefinitionDto> Steps { get; set; } = new();
    public bool IsActive { get; set; }
}

public class StepHandlerInfoDto
{
    public string HandlerType { get; set; } = string.Empty;
    public string StepType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

