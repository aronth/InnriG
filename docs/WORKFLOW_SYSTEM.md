# Workflow System Documentation

## Overview

The Workflow System is an extensible, step-based automation engine for processing emails in the InnriGreifi application. It allows you to define automated workflows that trigger based on email classifications, execute a series of steps, and pause for user approval when needed.

## Table of Contents

1. [Architecture](#architecture)
2. [Core Concepts](#core-concepts)
3. [Workflow Lifecycle](#workflow-lifecycle)
4. [Creating Workflows](#creating-workflows)
5. [Creating Step Handlers](#creating-step-handlers)
6. [Approval Gates](#approval-gates)
7. [Frontend Integration](#frontend-integration)
8. [Data Flow](#data-flow)
9. [Examples](#examples)
10. [Troubleshooting](#troubleshooting)

---

## Architecture

### Components

The workflow system consists of several key components:

1. **WorkflowRegistry** - Static registry that maps email classifications to workflow definitions
2. **WorkflowExecutionService** - Core engine that executes workflows and manages state
3. **WorkflowDefinition** - Template defining the steps for a workflow
4. **WorkflowInstance** - Active instance of a workflow for a specific conversation
5. **IWorkflowStepHandler** - Interface for implementing workflow steps
6. **WorkflowStepExecution** - Log of each step's execution
7. **WorkflowApproval** - Records user approvals for workflow steps

### Database Schema

```
WorkflowInstance
├── Id (Guid)
├── ConversationId (Guid) → EmailConversation
├── WorkflowType (string) - e.g., "CreditIssuance"
├── State (string) - Pending, InProgress, AwaitingApproval, Completed, Failed
├── CurrentStepIndex (int)
├── WorkflowDataJson (string) - Flexible JSON storage
├── StepExecutions (List<WorkflowStepExecution>)
└── Approvals (List<WorkflowApproval>)
```

---

## Core Concepts

### Workflow Definition

A `WorkflowDefinition` is a template that describes:
- **Name**: Unique identifier (e.g., "CreditIssuance")
- **Classification**: Email classification that triggers this workflow (e.g., "Complaint")
- **Steps**: Ordered list of `WorkflowStepDefinition` objects

### Workflow Instance

A `WorkflowInstance` represents an active workflow for a specific email conversation. It tracks:
- Current state (Pending, InProgress, AwaitingApproval, Completed, Failed)
- Current step index
- Workflow-specific data stored as JSON

### Step Handler

A step handler is a C# class that implements `IWorkflowStepHandler` and executes a specific action:
- Receives: workflow instance, conversation, extracted data, configuration
- Returns: `WorkflowStepResult` with success status, output data, and approval requirements

### Workflow States

- **Pending**: Workflow created but not started
- **InProgress**: Currently executing steps
- **AwaitingApproval**: Paused, waiting for user approval
- **Completed**: All steps finished successfully
- **Failed**: Workflow encountered an error

---

## Workflow Lifecycle

### 1. Email Classification

When an email is classified by the `EmailClassificationService`, the classification is stored on the `EmailConversation`.

### 2. Workflow Initialization

After classification, `EmailClassificationBackgroundService` calls:
```csharp
await workflowService.InitializeWorkflowAsync(conversationId, classification, ct);
```

This method:
1. Checks if a workflow already exists for the conversation
2. Looks up the workflow definition for the classification
3. Creates a new `WorkflowInstance` if a definition exists
4. Immediately starts execution

### 3. Workflow Execution

`ExecuteWorkflowAsync` runs the workflow:
1. Loads the workflow instance and definition
2. Iterates through steps in order
3. For each step:
   - Resolves the step handler from DI container
   - Creates a `WorkflowStepExecution` record
   - Executes the handler
   - Merges output data into workflow data
   - If step requires approval, pauses workflow
4. Updates state to "Completed" when all steps finish

### 4. Approval Process

When a step requires approval:
1. Workflow state changes to "AwaitingApproval"
2. A `WorkflowApproval` record is created with status "Pending"
3. Frontend displays approval interface
4. User approves/rejects via API
5. Workflow resumes or fails based on user action

---

## Creating Workflows

### Step 1: Define Workflow Definition

Create a static class that returns a `WorkflowDefinition`:

```csharp
// backend/Services/TableBookingWorkflowDefinition.cs
using InnriGreifi.API.Models;

namespace InnriGreifi.API.Services;

public static class TableBookingWorkflowDefinition
{
    public static WorkflowDefinition GetDefinition()
    {
        return new WorkflowDefinition
        {
            Name = "TableBooking",
            Classification = "TableBooking",
            Steps = new List<WorkflowStepDefinition>
            {
                new()
                {
                    StepType = "AvailabilityCheck",
                    HandlerType = "InnriGreifi.API.Services.Steps.AvailabilityCheckStepHandler",
                    Order = 1,
                    RequiresApproval = false
                },
                new()
                {
                    StepType = "BookingCreation",
                    HandlerType = "InnriGreifi.API.Services.Steps.BookingCreationStepHandler",
                    Order = 2,
                    RequiresApproval = true // User must approve before creating booking
                },
                new()
                {
                    StepType = "ConfirmationEmail",
                    HandlerType = "InnriGreifi.API.Services.Steps.ConfirmationEmailStepHandler",
                    Order = 3,
                    RequiresApproval = false
                }
            }
        };
    }
}
```

### Step 2: Register Workflow

In `Program.cs`, register the workflow:

```csharp
// Register workflow definition
WorkflowRegistry.RegisterWorkflow(TableBookingWorkflowDefinition.GetDefinition());
```

### Step 3: Create Step Handlers

See [Creating Step Handlers](#creating-step-handlers) below.

---

## Creating Step Handlers

### Step Handler Interface

```csharp
public interface IWorkflowStepHandler
{
    Task<WorkflowStepResult> ExecuteAsync(
        WorkflowInstance workflow,
        EmailConversation conversation,
        EmailExtractedData? extractedData,
        Dictionary<string, object> configuration,
        CancellationToken ct = default);
}
```

### Example: Order Lookup Step

```csharp
// backend/Services/Steps/OrderLookupStepHandler.cs
using InnriGreifi.API.Models;

namespace InnriGreifi.API.Services.Steps;

public class OrderLookupStepHandler : IWorkflowStepHandler
{
    private readonly IGreifinnOrderScraper _orderScraper;
    private readonly ILogger<OrderLookupStepHandler> _logger;

    public OrderLookupStepHandler(
        IGreifinnOrderScraper orderScraper,
        ILogger<OrderLookupStepHandler> logger)
    {
        _orderScraper = orderScraper;
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
            // 1. Extract data from previous steps or extracted data
            var phone = extractedData?.ContactPhone;
            var date = extractedData?.RequestedDate;

            // 2. Perform the step's action
            var ordersResult = await _orderScraper.GetOrdersAsync(
                phoneNumber: phone,
                fromDate: date ?? DateTime.UtcNow.AddDays(-7),
                toDate: date?.AddDays(1) ?? DateTime.UtcNow,
                page: 1,
                pageSize: 50,
                cancellationToken: ct);

            // 3. Return result with output data
            return new WorkflowStepResult
            {
                Success = true,
                OutputData = new Dictionary<string, object>
                {
                    ["MatchedOrders"] = ordersResult.Orders,
                    ["MatchCount"] = ordersResult.Orders.Count,
                    ["SearchPhone"] = phone ?? string.Empty
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OrderLookupStepHandler");
            return new WorkflowStepResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
```

### Accessing Workflow Data

To read data from previous steps:

```csharp
// Deserialize workflow data
var workflowData = JsonSerializer.Deserialize<Dictionary<string, object>>(
    workflow.WorkflowDataJson ?? "{}") ?? new Dictionary<string, object>();

// Extract values (handle JsonElement types)
if (workflowData.TryGetValue("SelectedOrderId", out var orderIdObj))
{
    var orderId = ExtractString(orderIdObj); // Helper method
}
```

### Helper Methods for JSON Extraction

When working with `Dictionary<string, object>` from JSON, values may be `JsonElement`:

```csharp
private string? ExtractString(object? value)
{
    if (value == null) return null;
    if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.String)
        return jsonElement.GetString();
    if (value is string str) return str;
    return value.ToString();
}

private decimal ExtractDecimal(object? value)
{
    if (value == null) return 0m;
    if (value is JsonElement jsonElement)
    {
        if (jsonElement.ValueKind == JsonValueKind.Number)
            return jsonElement.GetDecimal();
        if (jsonElement.ValueKind == JsonValueKind.String && decimal.TryParse(jsonElement.GetString(), out var d))
            return d;
    }
    if (value is decimal d) return d;
    if (decimal.TryParse(value.ToString(), out var result)) return result;
    return 0m;
}
```

### Register Step Handler

In `Program.cs`:

```csharp
// Register step handlers
builder.Services.AddScoped<IWorkflowStepHandler, OrderLookupStepHandler>();
builder.Services.AddScoped<IWorkflowStepHandler, OrderVerificationStepHandler>();
// ... etc
```

**Important**: The handler type name in `WorkflowStepDefinition.HandlerType` must match the fully qualified class name.

---

## Approval Gates

### Creating an Approval Step

Use the `ApprovalStepHandler` or create a custom one:

```csharp
public class ApprovalStepHandler : IWorkflowStepHandler
{
    public async Task<WorkflowStepResult> ExecuteAsync(
        WorkflowInstance workflow,
        EmailConversation conversation,
        EmailExtractedData? extractedData,
        Dictionary<string, object> configuration,
        CancellationToken ct = default)
    {
        return new WorkflowStepResult
        {
            Success = true,
            RequiresApproval = true,
            ApprovalPrompt = "Please review the proposed credit amount and approve or reject."
        };
    }
}
```

### Marking Step as Requiring Approval

In the workflow definition:

```csharp
new()
{
    StepType = "Approval",
    HandlerType = "InnriGreifi.API.Services.Steps.ApprovalStepHandler",
    Order = 5,
    RequiresApproval = true // This marks the step as requiring approval
}
```

### Approving/Rejecting via API

**Approve:**
```http
POST /api/workflows/{workflowInstanceId}/approve
Content-Type: application/json

{
  "approvalData": {
    "comments": "Looks good"
  }
}
```

**Reject:**
```http
POST /api/workflows/{workflowInstanceId}/reject
Content-Type: application/json

{
  "comments": "Credit amount seems too high"
}
```

### Frontend Approval

The `WorkflowApproval` component automatically displays when `workflow.state === 'AwaitingApproval'` and handles user interactions.

---

## Frontend Integration

### Components

1. **WorkflowSidebar** (`frontend/app/components/WorkflowSidebar.vue`)
   - Displays workflow status and data
   - Shows approval interface when needed
   - Collapsible sidebar
   - Auto-refreshes when workflow is active

2. **WorkflowStatus** (`frontend/app/components/WorkflowStatus.vue`)
   - Visual progress indicator
   - Shows step statuses (Pending, Running, Completed, Failed)
   - Displays step results

3. **WorkflowApproval** (`frontend/app/components/WorkflowApproval.vue`)
   - Approval/rejection interface
   - Shows approval prompt and data
   - Handles user actions

### API Integration

The `useWorkflows` composable provides:

```typescript
const { 
  getWorkflowByConversation,
  approveWorkflow,
  rejectWorkflow 
} = useWorkflows()

// Get workflow for a conversation
const workflow = await getWorkflowByConversation(conversationId)

// Approve workflow
await approveWorkflow(workflowInstanceId, { comments: "Approved" })

// Reject workflow
await rejectWorkflow(workflowInstanceId, "Rejected: reason")
```

### Displaying Workflow Data

Workflow data is available in `workflow.workflowData`:

```vue
<div v-if="workflow.workflowData?.SelectedOrderId">
  Order: {{ workflow.workflowData.SelectedOrderId }}
</div>
<div v-if="workflow.workflowData?.ProposedCreditAmount">
  Credit: {{ formatCurrency(workflow.workflowData.ProposedCreditAmount) }}
</div>
```

---

## Data Flow

### 1. Email Received
```
Email → EmailPollingBackgroundService → EmailMessage → EmailConversation
```

### 2. Classification
```
EmailConversation → EmailClassificationService → Classification stored
```

### 3. Workflow Triggered
```
Classification → WorkflowRegistry → WorkflowDefinition → WorkflowInstance created
```

### 4. Step Execution
```
WorkflowInstance → WorkflowExecutionService → Step Handler → WorkflowStepResult
                                                              ↓
                                                    OutputData merged into WorkflowDataJson
```

### 5. Approval (if needed)
```
WorkflowStepResult (RequiresApproval=true) → WorkflowInstance.State = "AwaitingApproval"
                                                              ↓
                                                    User approves/rejects via API
                                                              ↓
                                                    Workflow resumes or fails
```

### 6. Completion
```
All steps complete → WorkflowInstance.State = "Completed"
```

---

## Examples

### Example 1: Credit Issuance Workflow

**Trigger**: Email classified as "Complaint"

**Steps**:
1. **OrderLookup** - Find orders by phone/date
2. **OrderVerification** - AI matches email to order
3. **CreditCalculation** - AI extracts credit amount
4. **ResponseDraft** - AI drafts customer response
5. **Approval** - User reviews and approves
6. **CreditIssuance** - Credit issued to phone number
7. **EmailSend** - Response sent to customer

**Data Flow**:
- Step 1 outputs: `MatchedOrders`, `MatchCount`
- Step 2 outputs: `SelectedOrderId`, `MatchConfidence`
- Step 3 outputs: `ProposedCreditAmount`
- Step 4 outputs: `DraftResponse`
- Step 5: Pauses for approval
- Step 6: Uses `SelectedOrderId` and `ProposedCreditAmount`
- Step 7: Uses `DraftResponse`

### Example 2: Table Booking Workflow (Future)

**Trigger**: Email classified as "TableBooking"

**Steps**:
1. **AvailabilityCheck** - Check table availability
2. **BookingCreation** - Create booking (requires approval)
3. **ConfirmationEmail** - Send confirmation

---

## Troubleshooting

### Workflow Not Triggering

1. **Check classification**: Ensure email is classified correctly
2. **Check registry**: Verify workflow is registered in `Program.cs`
3. **Check logs**: Look for "No workflow defined for classification" messages

### Step Handler Not Found

1. **Check handler type**: Ensure `HandlerType` matches fully qualified class name
2. **Check registration**: Verify handler is registered in DI container
3. **Check namespace**: Ensure namespace matches

### JSON Deserialization Errors

When accessing `workflowData`, values may be `JsonElement`:
- Use helper methods (`ExtractString`, `ExtractDecimal`, etc.)
- Check `JsonValueKind` before accessing
- Handle both `JsonElement` and direct types

### Approval Not Working

1. **Check state**: Workflow must be in "AwaitingApproval" state
2. **Check step**: Step must have `RequiresApproval = true`
3. **Check API**: Verify approval endpoint is called correctly
4. **Check user**: User must be authenticated

### Workflow Stuck

1. **Check state**: Look at `WorkflowInstance.State`
2. **Check step executions**: Review `WorkflowStepExecution` records
3. **Check logs**: Look for error messages in step execution
4. **Manual intervention**: May need to update state manually in database

---

## Best Practices

### 1. Step Design
- Keep steps focused on a single action
- Make steps reusable across workflows
- Handle errors gracefully
- Log important actions

### 2. Data Storage
- Store only necessary data in `WorkflowDataJson`
- Use clear, consistent key names
- Document data structure in comments

### 3. Error Handling
- Always return `WorkflowStepResult` with appropriate success/error
- Log errors with context
- Provide meaningful error messages

### 4. Testing
- Test each step handler independently
- Test workflow end-to-end
- Test approval flow
- Test error scenarios

### 5. Performance
- Avoid blocking operations in step handlers
- Use async/await properly
- Consider timeout for long-running steps

---

## API Reference

### Endpoints

**Get Workflow by Conversation**
```http
GET /api/workflows/conversations/{conversationId}
```

**Get Workflow Instance**
```http
GET /api/workflows/{workflowInstanceId}
```

**Approve Workflow**
```http
POST /api/workflows/{workflowInstanceId}/approve
Body: { "approvalData": {...}, "comments": "..." }
```

**Reject Workflow**
```http
POST /api/workflows/{workflowInstanceId}/reject
Body: { "comments": "..." }
```

**Get Pending Approvals**
```http
GET /api/workflows/pending-approvals
```

---

## Future Enhancements

Potential improvements to consider:

1. **Conditional Steps**: Steps that execute based on conditions
2. **Parallel Steps**: Execute multiple steps simultaneously
3. **Retry Logic**: Automatic retry for failed steps
4. **Step Timeouts**: Timeout handling for long-running steps
5. **Workflow Templates**: UI for creating workflows
6. **Workflow History**: Detailed audit trail
7. **Notifications**: Alert users when approval needed
8. **Workflow Analytics**: Track workflow performance

---

## Related Files

### Backend
- `backend/Services/WorkflowExecutionService.cs` - Core execution engine
- `backend/Services/WorkflowRegistry.cs` - Workflow registry
- `backend/Models/WorkflowDefinition.cs` - Workflow definition models
- `backend/Models/WorkflowInstance.cs` - Workflow instance model
- `backend/Services/IWorkflowStepHandler.cs` - Step handler interface
- `backend/Services/Steps/*.cs` - Step handler implementations
- `backend/Controllers/WorkflowsController.cs` - API endpoints

### Frontend
- `frontend/app/components/WorkflowSidebar.vue` - Sidebar component
- `frontend/app/components/WorkflowStatus.vue` - Status display
- `frontend/app/components/WorkflowApproval.vue` - Approval interface
- `frontend/app/composables/useWorkflows.ts` - API composable
- `frontend/app/types/workflow.ts` - TypeScript types

---

**Last Updated**: 2025-01-23
**Version**: 1.0.0

