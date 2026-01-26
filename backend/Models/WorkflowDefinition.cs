namespace InnriGreifi.API.Models;

public class WorkflowDefinition
{
    public string Name { get; set; } = string.Empty;

    public string Classification { get; set; } = string.Empty; // Maps to email classification

    public List<WorkflowStepDefinition> Steps { get; set; } = new();
}

public class WorkflowStepDefinition
{
    public string StepType { get; set; } = string.Empty; // e.g., "OrderLookup"

    public string HandlerType { get; set; } = string.Empty; // Fully qualified type name

    public int Order { get; set; }

    public bool RequiresApproval { get; set; }

    public Dictionary<string, object> Configuration { get; set; } = new();
}

