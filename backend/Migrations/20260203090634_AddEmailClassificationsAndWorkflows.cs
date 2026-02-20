using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnriGreifi.API.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailClassificationsAndWorkflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailClassifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SystemPrompt = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailClassifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ClassificationId = table.Column<Guid>(type: "uuid", nullable: true),
                    StepsJson = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowDefinitions_EmailClassifications_ClassificationId",
                        column: x => x.ClassificationId,
                        principalTable: "EmailClassifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailClassifications_IsActive",
                table: "EmailClassifications",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_EmailClassifications_IsSystem",
                table: "EmailClassifications",
                column: "IsSystem");

            migrationBuilder.CreateIndex(
                name: "IX_EmailClassifications_Name",
                table: "EmailClassifications",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinitions_ClassificationId",
                table: "WorkflowDefinitions",
                column: "ClassificationId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinitions_IsActive",
                table: "WorkflowDefinitions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinitions_Name",
                table: "WorkflowDefinitions",
                column: "Name",
                unique: true);

            // Seed system classifications
            var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var generalInquiryId = Guid.NewGuid().ToString();
            var sponsorshipId = Guid.NewGuid().ToString();
            var marketingId = Guid.NewGuid().ToString();
            var accountingId = Guid.NewGuid().ToString();
            var groupBookingId = Guid.NewGuid().ToString();
            var buffetBookingId = Guid.NewGuid().ToString();
            var tableBookingId = Guid.NewGuid().ToString();
            var complaintId = Guid.NewGuid().ToString();

            migrationBuilder.Sql($@"
                INSERT INTO ""EmailClassifications"" (""Id"", ""Name"", ""Description"", ""SystemPrompt"", ""IsSystem"", ""IsActive"", ""CreatedAt"", ""UpdatedAt"")
                VALUES
                    ('{generalInquiryId}', 'GeneralInquiry', 'Almennt fyrirspurn: Almennar fyrirspurnir sem ekki falla undir aðra flokka', NULL, true, true, '{now}', '{now}'),
                    ('{sponsorshipId}', 'Sponsorship', 'Vöruliðun: Beiðnir um vöruliðun eða samstarf', NULL, true, true, '{now}', '{now}'),
                    ('{marketingId}', 'Marketing', 'Markaðssetning: Fyrirspurnir um markaðssetningu eða auglýsingar', NULL, true, true, '{now}', '{now}'),
                    ('{accountingId}', 'Accounting', 'Bókhald: Fyrirspurnir um reikninga eða bókhald', NULL, true, true, '{now}', '{now}'),
                    ('{groupBookingId}', 'GroupBooking', 'Hópabókun: Bókun fyrir hópa 15 mans og fleiri', NULL, true, true, '{now}', '{now}'),
                    ('{buffetBookingId}', 'BuffetBooking', 'Borðhúsbókun: Bókun fyrir borðhús', NULL, true, true, '{now}', '{now}'),
                    ('{tableBookingId}', 'TableBooking', 'Borðbókun: Bókun fyrir borð', NULL, true, true, '{now}', '{now}'),
                    ('{complaintId}', 'Complaint', 'Kvörtun: Eitthvað hefur farið úrsekiðis og þarf að bæta kúnnanum mistökin', NULL, true, true, '{now}', '{now}');
            ");

            // Seed CreditIssuance workflow
            var workflowId = Guid.NewGuid().ToString();
            var stepsJson = @"[{""StepType"":""OrderLookup"",""HandlerType"":""InnriGreifi.API.Services.Steps.OrderLookupStepHandler"",""Order"":1,""RequiresApproval"":false,""Configuration"":{}},{""StepType"":""OrderVerification"",""HandlerType"":""InnriGreifi.API.Services.Steps.OrderVerificationStepHandler"",""Order"":2,""RequiresApproval"":false,""Configuration"":{}},{""StepType"":""CreditCalculation"",""HandlerType"":""InnriGreifi.API.Services.Steps.CreditCalculationStepHandler"",""Order"":3,""RequiresApproval"":false,""Configuration"":{}},{""StepType"":""ResponseDraft"",""HandlerType"":""InnriGreifi.API.Services.Steps.ResponseDraftStepHandler"",""Order"":4,""RequiresApproval"":false,""Configuration"":{}},{""StepType"":""Approval"",""HandlerType"":""InnriGreifi.API.Services.Steps.ApprovalStepHandler"",""Order"":5,""RequiresApproval"":true,""Configuration"":{}},{""StepType"":""CreditIssuance"",""HandlerType"":""InnriGreifi.API.Services.Steps.CreditIssuanceStepHandler"",""Order"":6,""RequiresApproval"":false,""Configuration"":{}},{""StepType"":""EmailSend"",""HandlerType"":""InnriGreifi.API.Services.Steps.EmailSendStepHandler"",""Order"":7,""RequiresApproval"":false,""Configuration"":{}}]";

            migrationBuilder.Sql($@"
                INSERT INTO ""WorkflowDefinitions"" (""Id"", ""Name"", ""ClassificationId"", ""StepsJson"", ""IsActive"", ""CreatedAt"", ""UpdatedAt"")
                VALUES ('{workflowId}', 'CreditIssuance', '{complaintId}', '{stepsJson.Replace("'", "''")}', true, '{now}', '{now}');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowDefinitions");

            migrationBuilder.DropTable(
                name: "EmailClassifications");
        }
    }
}
