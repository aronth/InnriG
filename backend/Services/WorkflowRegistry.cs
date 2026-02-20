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

    [Obsolete("WorkflowRegistry is deprecated. Use database-driven WorkflowDefinitions instead.")]
    public static void RegisterWorkflow(WorkflowDefinition definition, string classificationName)
    {
        _workflows[classificationName] = definition;
    }

    [Obsolete("WorkflowRegistry is deprecated. Use database-driven WorkflowDefinitions instead. This method is kept for backward compatibility during migration.")]
    public static WorkflowDefinition? GetWorkflowForClassification(string classification)
    {
        return _workflows.TryGetValue(classification, out var workflow) ? workflow : null;
    }

    public static bool HasWorkflowForClassification(string classification)
    {
        return _workflows.ContainsKey(classification);
    }
}

