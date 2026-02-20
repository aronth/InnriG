using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace InnriGreifi.API.Models;

public class WorkflowDefinition
{
    public Guid Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public Guid? ClassificationId { get; set; } // FK to EmailClassification (nullable for manual workflows)

    public string StepsJson { get; set; } = "[]"; // JSON array of WorkflowStepDefinition

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public EmailClassification? Classification { get; set; }

    // Helper property to deserialize StepsJson
    [System.Text.Json.Serialization.JsonIgnore]
    public List<WorkflowStepDefinition> Steps
    {
        get
        {
            if (string.IsNullOrEmpty(StepsJson))
                return new List<WorkflowStepDefinition>();

            try
            {
                return JsonSerializer.Deserialize<List<WorkflowStepDefinition>>(StepsJson) ?? new List<WorkflowStepDefinition>();
            }
            catch
            {
                return new List<WorkflowStepDefinition>();
            }
        }
        set
        {
            StepsJson = JsonSerializer.Serialize(value);
        }
    }
}

public class WorkflowStepDefinition
{
    public string StepType { get; set; } = string.Empty; // e.g., "OrderLookup"

    public string HandlerType { get; set; } = string.Empty; // Fully qualified type name

    public int Order { get; set; }

    public bool RequiresApproval { get; set; }

    public Dictionary<string, object> Configuration { get; set; } = new();
}

