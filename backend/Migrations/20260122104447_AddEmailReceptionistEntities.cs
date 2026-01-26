using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnriGreifi.API.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailReceptionistEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailConversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GraphConversationId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    GraphThreadId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FromEmail = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    FromName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Classification = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MessageCount = table.Column<int>(type: "integer", nullable: false),
                    LastMessageId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LastMessageReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailConversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailConversations_AspNetUsers_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UserEmailMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailAddress = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEmailMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserEmailMappings_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmailMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    GraphMessageId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    GraphConversationId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    InReplyToId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FromEmail = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    FromName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ToEmail = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ToName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Cc = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Bcc = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReceivedDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    IsOutgoing = table.Column<bool>(type: "boolean", nullable: false),
                    IsSentFromSystem = table.Column<bool>(type: "boolean", nullable: false),
                    SentByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    HasAttachments = table.Column<bool>(type: "boolean", nullable: false),
                    AttachmentCount = table.Column<int>(type: "integer", nullable: false),
                    Importance = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Flag = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailMessages_AspNetUsers_SentByUserId",
                        column: x => x.SentByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EmailMessages_EmailConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "EmailConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmailAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    GraphAttachmentId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    IsInline = table.Column<bool>(type: "boolean", nullable: false),
                    ContentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailAttachments_EmailMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "EmailMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmailClassificationQueues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    QueuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailClassificationQueues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailClassificationQueues_EmailMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "EmailMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmailExtractedData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExtractedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Classification = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric", nullable: false),
                    RequestedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RequestedTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    GuestCount = table.Column<int>(type: "integer", nullable: true),
                    AdultCount = table.Column<int>(type: "integer", nullable: true),
                    ChildCount = table.Column<int>(type: "integer", nullable: true),
                    LocationCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SpecialRequests = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ContactPhone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ContactEmail = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ExtractedJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailExtractedData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailExtractedData_EmailConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "EmailConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmailExtractedData_EmailMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "EmailMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailAttachments_MessageId",
                table: "EmailAttachments",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailClassificationQueues_MessageId",
                table: "EmailClassificationQueues",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailClassificationQueues_QueuedAt",
                table: "EmailClassificationQueues",
                column: "QueuedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmailClassificationQueues_Status",
                table: "EmailClassificationQueues",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EmailConversations_AssignedToUserId",
                table: "EmailConversations",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailConversations_GraphConversationId",
                table: "EmailConversations",
                column: "GraphConversationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailConversations_Status",
                table: "EmailConversations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EmailExtractedData_ConversationId",
                table: "EmailExtractedData",
                column: "ConversationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailExtractedData_MessageId",
                table: "EmailExtractedData",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_ConversationId",
                table: "EmailMessages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_GraphConversationId",
                table: "EmailMessages",
                column: "GraphConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_GraphMessageId",
                table: "EmailMessages",
                column: "GraphMessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailMessages_SentByUserId",
                table: "EmailMessages",
                column: "SentByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserEmailMappings_UserId",
                table: "UserEmailMappings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserEmailMappings_UserId_EmailAddress",
                table: "UserEmailMappings",
                columns: new[] { "UserId", "EmailAddress" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailAttachments");

            migrationBuilder.DropTable(
                name: "EmailClassificationQueues");

            migrationBuilder.DropTable(
                name: "EmailExtractedData");

            migrationBuilder.DropTable(
                name: "UserEmailMappings");

            migrationBuilder.DropTable(
                name: "EmailMessages");

            migrationBuilder.DropTable(
                name: "EmailConversations");
        }
    }
}
