# Workflow Development Guide

## Overview

This guide provides step-by-step instructions for creating new workflows and step handlers in the InnriGreifi system. The workflow system is modular and extensible, allowing you to define custom workflows that trigger based on email classifications.

## Table of Contents

1. [Creating a New Workflow Type](#creating-a-new-workflow-type)
2. [Creating a New Step Handler](#creating-a-new-step-handler)
3. [Step Handler Best Practices](#step-handler-best-practices)
4. [Frontend Customization](#frontend-customization)
5. [Common Patterns](#common-patterns)
6. [Troubleshooting](#troubleshooting)

---

## Creating a New Workflow Type

### Step 1: Define the Workflow in the Database

Workflows are defined in the database via the `WorkflowDefinitions` table. You can create them through:

1. **UI**: Navigate to Settings > Workflow Definitions
2. **API**: `POST /api/workflow-definitions`

#### Required Fields

- **Name**: Unique identifier (e.g., "TableBooking", "RefundRequest")
- **ClassificationId**: (Optional) Links to an email classification that triggers this workflow
- **Steps**: Array of step definitions (see below)
- **IsActive**: Boolean flag to enable/disable the workflow

#### Step Definition Structure

Each step in a workflow requires:

```json
{
  "stepType": "AvailabilityCheck",
  "handlerType": "InnriGreifi.API.Services.Steps.AvailabilityCheckStepHandler",
  "order": 1,
  "requiresApproval": false,
  "configuration": {}
}
```

- **stepType**: Unique identifier for the step (used for display names)
- **handlerType**: Fully qualified C# class name of the step handler
- **order**: Execution order (1, 2, 3, ...)
- **requiresApproval**: Whether this step pauses for user approval
- **configuration**: Optional key-value pairs passed to the step handler

### Step 2: Add Frontend Step Name Translation

**File**: `frontend/app/composables/useWorkflowStepNames.ts`

Add your step type to the `stepNameMap`:

```typescript
const stepNameMap: Record<string, string> = {
  // ... existing steps
  AvailabilityCheck: 'Athuga lausn',
  BookingCreation: 'Stofna bókun',
  // Add your new step types here
}
```

### Step 3: Test the Workflow

1. Create a test email conversation with the appropriate classification
2. Verify the workflow initializes correctly
3. Check that steps execute in the correct order
4. Test approval gates if applicable
5. Verify frontend displays steps correctly

---

## Creating a New Step Handler

### Step 1: Create the Step Handler Class

**Location**: `backend/Services/Steps/YourStepHandler.cs`

```csharp
using InnriGreifi.API.Models;

namespace InnriGreifi.API.Services.Steps;

public class YourStepHandler : IWorkflowStepHandler
{
    private readonly ILogger<YourStepHandler> _logger;
    // Add other dependencies via constructor injection

    public YourStepHandler(
        ILogger<YourStepHandler> logger)
    {
        _logger = logger;
    }

    public async Task<WorkflowStepResult> ExecuteAsync(
        WorkflowInstance workflow,
        EmailConversation conversation,
        EmailExtractedData? extractedData,
        Dictionary<string, object> configuration,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("YourStepHandler: Executing for workflow {WorkflowInstanceId}", workflow.Id);

            // 1. Extract data from previous steps or extracted data
            var workflowData = DeserializeWorkflowData(workflow.WorkflowDataJson);
            
            // 2. Perform the step's action
            // ... your logic here ...

            // 3. Return result with output data
            return new WorkflowStepResult
            {
                Success = true,
                OutputData = new Dictionary<string, object>
                {
                    ["YourOutputKey"] = yourOutputValue
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in YourStepHandler for workflow {WorkflowInstanceId}", workflow.Id);
            return new WorkflowStepResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
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
}
```

### Step 2: Register Handler in Dependency Injection

**File**: `backend/Program.cs`

Add your handler to the service collection:

```csharp
builder.Services.AddScoped<IWorkflowStepHandler, YourStepHandler>();
```

Or use the interface directly (handlers are auto-discovered):

```csharp
builder.Services.AddScoped<YourStepHandler>();
```

### Step 3: Add Frontend Display Name

**File**: `frontend/app/composables/useWorkflowStepNames.ts`

```typescript
const stepNameMap: Record<string, string> = {
  // ... existing
  YourStep: 'Your Icelandic Display Name',
}
```

### Step 4: (Optional) Add Step Result Formatting

**File**: `frontend/app/composables/useWorkflowStepResults.ts`

If your step returns specific result data that should be formatted:

```typescript
const formatStepResult = (stepType: string, result: Record<string, any>): string => {
  // ... existing patterns
  if (stepType === 'YourStep' && result.YourResultKey !== undefined) {
    return `Your formatted result: ${result.YourResultKey}`
  }
  return ''
}
```

---

## Step Handler Best Practices

### Input Validation

Always validate inputs before processing:

```csharp
if (extractedData == null)
{
    return new WorkflowStepResult
    {
        Success = false,
        ErrorMessage = "No extracted data available"
    };
}

var requiredField = ExtractString(workflowData, "RequiredField");
if (string.IsNullOrEmpty(requiredField))
{
    return new WorkflowStepResult
    {
        Success = false,
        ErrorMessage = "RequiredField is missing"
    };
}
```

### Error Handling

- Always catch exceptions and return meaningful error messages
- Log errors with context (workflow ID, step type, etc.)
- Don't throw exceptions - return `WorkflowStepResult` with `Success = false`

```csharp
try
{
    // Your logic
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error in YourStepHandler for workflow {WorkflowInstanceId}", workflow.Id);
    return new WorkflowStepResult
    {
        Success = false,
        ErrorMessage = ex.Message // Or a user-friendly message
    };
}
```

### Output Data Structure

- Use clear, descriptive keys for output data
- Store data that subsequent steps might need
- Use consistent naming conventions (PascalCase)
- Avoid storing large objects - store IDs or summaries instead

```csharp
OutputData = new Dictionary<string, object>
{
    ["SelectedOrderId"] = orderId,
    ["MatchCount"] = orders.Count,
    ["MatchConfidence"] = confidenceScore
}
```

### Approval Requirements

If your step requires user approval:

```csharp
return new WorkflowStepResult
{
    Success = true,
    RequiresApproval = true,
    ApprovalPrompt = "Please review and approve the following:",
    OutputData = new Dictionary<string, object>
    {
        ["ProposedAction"] = actionDetails
    }
};
```

The workflow will pause at this step and wait for user approval via the frontend.

### Accessing Previous Step Data

To read data from previous steps:

```csharp
var workflowData = DeserializeWorkflowData(workflow.WorkflowDataJson);

// Extract values (handle JsonElement types)
var previousStepData = ExtractString(workflowData, "PreviousStepOutput");
var numericValue = ExtractNumber(workflowData, "NumericField");
```

### Helper Methods for JSON Extraction

When working with `Dictionary<string, object>` from JSON, values may be `JsonElement`:

```csharp
private string? ExtractString(object? value)
{
    if (value == null) return null;
    if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.String)
        return jsonElement.GetString();
    return value.ToString();
}

private decimal? ExtractNumber(object? value)
{
    if (value == null) return null;
    if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Number)
        return jsonElement.GetDecimal();
    if (decimal.TryParse(value.ToString(), out var result))
        return result;
    return null;
}
```

---

## Frontend Customization

### Adding Step Display Names

**File**: `frontend/app/composables/useWorkflowStepNames.ts`

Add your step type to the mapping:

```typescript
const stepNameMap: Record<string, string> = {
  YourStepType: 'Your Icelandic Name',
}
```

### Custom Step Result Formatting

**File**: `frontend/app/composables/useWorkflowStepResults.ts`

Add formatting logic for your step's result data:

```typescript
const formatStepResult = (stepType: string, result: Record<string, any>): string => {
  if (stepType === 'YourStep' && result.YourKey !== undefined) {
    // Format your result
    return `Formatted: ${result.YourKey}`
  }
  return ''
}
```

### Approval UI Customization

The `WorkflowApproval` component automatically handles:
- Credit issuance workflows (backward compatibility)
- Generic workflows (dynamic field rendering)

For custom approval UIs, you can:
1. Extend `WorkflowApproval.vue` with workflow-type-specific sections
2. Create a new approval component and conditionally render it in `WorkflowSidebar.vue`

### Workflow Data Display

The workflow sidebar automatically displays workflow data fields. To customize:

**File**: `frontend/app/components/WorkflowSidebar.vue`

- Add special formatting in `formatFieldValue()`
- Add action URLs in `getFieldActionUrl()`
- Filter fields in `filteredWorkflowData` computed property

---

## Common Patterns

### Pattern 1: Data Extraction Step

Extract data from email or previous steps:

```csharp
public async Task<WorkflowStepResult> ExecuteAsync(...)
{
    var phone = extractedData?.ContactPhone;
    var date = extractedData?.RequestedDate;
    
    // Process extracted data
    var processedData = ProcessData(phone, date);
    
    return new WorkflowStepResult
    {
        Success = true,
        OutputData = new Dictionary<string, object>
        {
            ["ProcessedData"] = processedData
        }
    };
}
```

### Pattern 2: External API Call

Call external services:

```csharp
public class ExternalApiStepHandler : IWorkflowStepHandler
{
    private readonly IHttpClientFactory _httpClientFactory;
    
    public async Task<WorkflowStepResult> ExecuteAsync(...)
    {
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync("https://api.example.com/data", ct);
        
        if (!response.IsSuccessStatusCode)
        {
            return new WorkflowStepResult
            {
                Success = false,
                ErrorMessage = $"API call failed: {response.StatusCode}"
            };
        }
        
        var data = await response.Content.ReadFromJsonAsync<YourDataType>();
        
        return new WorkflowStepResult
        {
            Success = true,
            OutputData = new Dictionary<string, object>
            {
                ["ApiData"] = data
            }
        };
    }
}
```

### Pattern 3: Approval Gate

Pause workflow for user approval:

```csharp
return new WorkflowStepResult
{
    Success = true,
    RequiresApproval = true,
    ApprovalPrompt = "Please review the proposed action:",
    OutputData = new Dictionary<string, object>
    {
        ["ProposedAction"] = actionDetails,
        ["EstimatedCost"] = cost
    }
};
```

When approved, the workflow continues. Approval data is merged into workflow data.

### Pattern 4: Email Sending

Send response email:

```csharp
public class EmailSendStepHandler : IWorkflowStepHandler
{
    private readonly IGraphEmailService _graphService;
    
    public async Task<WorkflowStepResult> ExecuteAsync(...)
    {
        var workflowData = DeserializeWorkflowData(workflow.WorkflowDataJson);
        var draftResponse = ExtractString(workflowData, "DraftResponse");
        
        if (string.IsNullOrEmpty(draftResponse))
        {
            return new WorkflowStepResult
            {
                Success = false,
                ErrorMessage = "No draft response found"
            };
        }
        
        await _graphService.SendEmailAsync(
            to: conversation.FromEmail,
            subject: $"Re: {conversation.Subject}",
            body: draftResponse,
            ct);
        
        return new WorkflowStepResult
        {
            Success = true
        };
    }
}
```

### Pattern 5: Conditional Logic

Execute different logic based on workflow data:

```csharp
var workflowData = DeserializeWorkflowData(workflow.WorkflowDataJson);
var condition = ExtractString(workflowData, "Condition");

if (condition == "OptionA")
{
    // Execute Option A logic
}
else if (condition == "OptionB")
{
    // Execute Option B logic
}
else
{
    return new WorkflowStepResult
    {
        Success = false,
        ErrorMessage = $"Unknown condition: {condition}"
    };
}
```

---

## Troubleshooting

### Handler Not Found

**Error**: `Handler {HandlerType} not found`

**Solutions**:
1. Verify handler class name matches `HandlerType` in workflow definition
2. Check handler is registered in DI (`Program.cs`)
3. Ensure handler implements `IWorkflowStepHandler`
4. Verify namespace matches fully qualified type name

### Steps Not Displaying

**Issue**: Steps don't appear in the workflow sidebar

**Solutions**:
1. Verify workflow definition is loaded (check browser console)
2. Ensure workflow definition has `IsActive = true`
3. Check step names are added to `useWorkflowStepNames.ts`
4. Verify workflow instance has correct `WorkflowType` matching definition name

### Approval Not Working

**Issue**: Approval UI doesn't appear or approval doesn't continue workflow

**Solutions**:
1. Verify step has `RequiresApproval = true` in definition
2. Check step handler returns `RequiresApproval = true` in result
3. Verify approval API endpoint is called correctly
4. Check workflow state transitions correctly after approval

### Workflow Data Not Persisting

**Issue**: Data from one step not available in next step

**Solutions**:
1. Verify step handler returns data in `OutputData` dictionary
2. Check data keys match what subsequent steps expect
3. Ensure workflow data is serialized correctly (use helper methods)
4. Verify workflow instance is saved after each step

### Step Execution Fails Silently

**Issue**: Step fails but workflow continues

**Solutions**:
1. Check step handler returns `Success = false` on errors
2. Verify error messages are logged
3. Check workflow execution service handles failures correctly
4. Review step execution records in database

### Frontend Not Updating

**Issue**: Workflow sidebar doesn't reflect latest state

**Solutions**:
1. Check polling is enabled for `AwaitingApproval` and `InProgress` states
2. Verify API endpoints return latest data
3. Check browser console for errors
4. Ensure workflow definition is fetched when workflow loads

---

## Quick Reference

### Step Handler Template

```csharp
public class YourStepHandler : IWorkflowStepHandler
{
    private readonly ILogger<YourStepHandler> _logger;

    public YourStepHandler(ILogger<YourStepHandler> logger)
    {
        _logger = logger;
    }

    public async Task<WorkflowStepResult> ExecuteAsync(
        WorkflowInstance workflow,
        EmailConversation conversation,
        EmailExtractedData? extractedData,
        Dictionary<string, object> configuration,
        CancellationToken ct = default)
    {
        try
        {
            // Your logic here
            return new WorkflowStepResult
            {
                Success = true,
                OutputData = new Dictionary<string, object>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in YourStepHandler");
            return new WorkflowStepResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
```

### Workflow Definition JSON Example

```json
{
  "name": "YourWorkflow",
  "classificationId": "guid-here",
  "steps": [
    {
      "stepType": "DataExtraction",
      "handlerType": "InnriGreifi.API.Services.Steps.DataExtractionStepHandler",
      "order": 1,
      "requiresApproval": false,
      "configuration": {}
    },
    {
      "stepType": "Processing",
      "handlerType": "InnriGreifi.API.Services.Steps.ProcessingStepHandler",
      "order": 2,
      "requiresApproval": true,
      "configuration": {
        "timeout": 30
      }
    }
  ]
}
```

---

## Additional Resources

- [Workflow System Documentation](./WORKFLOW_SYSTEM.md) - Architecture and system overview
- [Backend API Documentation](../backend/README.md) - API endpoints and services
- [Frontend Components](../frontend/app/components/README.md) - Component documentation

---

**Last Updated**: 2025-01-XX
**Version**: 1.0.0

