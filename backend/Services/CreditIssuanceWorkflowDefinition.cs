using InnriGreifi.API.Models;

namespace InnriGreifi.API.Services;

public static class CreditIssuanceWorkflowDefinition
{
    public static WorkflowDefinition GetDefinition()
    {
        // Note: ClassificationId will be set in migration when seeding to database
        // This method is kept for backward compatibility during migration
        return new WorkflowDefinition
        {
            Name = "CreditIssuance",
            // ClassificationId will be set when migrating to database
            Steps = new List<WorkflowStepDefinition>
            {
                new()
                {
                    StepType = "OrderLookup",
                    HandlerType = "InnriGreifi.API.Services.Steps.OrderLookupStepHandler",
                    Order = 1,
                    RequiresApproval = false
                },
                new()
                {
                    StepType = "OrderVerification",
                    HandlerType = "InnriGreifi.API.Services.Steps.OrderVerificationStepHandler",
                    Order = 2,
                    RequiresApproval = false
                },
                new()
                {
                    StepType = "CreditCalculation",
                    HandlerType = "InnriGreifi.API.Services.Steps.CreditCalculationStepHandler",
                    Order = 3,
                    RequiresApproval = false
                },
                new()
                {
                    StepType = "ResponseDraft",
                    HandlerType = "InnriGreifi.API.Services.Steps.ResponseDraftStepHandler",
                    Order = 4,
                    RequiresApproval = false
                },
                new()
                {
                    StepType = "Approval",
                    HandlerType = "InnriGreifi.API.Services.Steps.ApprovalStepHandler",
                    Order = 5,
                    RequiresApproval = true
                },
                new()
                {
                    StepType = "CreditIssuance",
                    HandlerType = "InnriGreifi.API.Services.Steps.CreditIssuanceStepHandler",
                    Order = 6,
                    RequiresApproval = false
                },
                new()
                {
                    StepType = "EmailSend",
                    HandlerType = "InnriGreifi.API.Services.Steps.EmailSendStepHandler",
                    Order = 7,
                    RequiresApproval = false
                }
            }
        };
    }
}

