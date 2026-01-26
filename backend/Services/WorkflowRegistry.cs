using InnriGreifi.API.Models;

namespace InnriGreifi.API.Services;

public class WorkflowRegistry
{
    private static readonly Dictionary<string, WorkflowDefinition> _workflows = new();

    static WorkflowRegistry()
    {
        // Register workflows here
        // For now, we'll register them when we create the workflow definitions
    }

    public static void RegisterWorkflow(WorkflowDefinition definition)
    {
        _workflows[definition.Classification] = definition;
    }

    public static WorkflowDefinition? GetWorkflowForClassification(string classification)
    {
        return _workflows.TryGetValue(classification, out var workflow) ? workflow : null;
    }

    public static bool HasWorkflowForClassification(string classification)
    {
        return _workflows.ContainsKey(classification);
    }
}

