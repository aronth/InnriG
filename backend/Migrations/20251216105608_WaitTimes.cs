using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnriGreifi.API.Migrations
{
    /// <inheritdoc />
    public partial class WaitTimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WaitTimeNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Restaurant = table.Column<int>(type: "integer", nullable: false),
                    PushoverUserKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PushoverAppToken = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SottThresholdMinutes = table.Column<int>(type: "integer", nullable: true),
                    SentThresholdMinutes = table.Column<int>(type: "integer", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LastNotifiedSott = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastNotifiedSent = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaitTimeNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaitTimeNotifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WaitTimeRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Restaurant = table.Column<int>(type: "integer", nullable: false),
                    SottMinutes = table.Column<int>(type: "integer", nullable: true),
                    SentMinutes = table.Column<int>(type: "integer", nullable: true),
                    IsClosed = table.Column<bool>(type: "boolean", nullable: false),
                    ScrapedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaitTimeRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WaitTimeNotifications_UserId_Restaurant",
                table: "WaitTimeNotifications",
                columns: new[] { "UserId", "Restaurant" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WaitTimeNotifications");

            migrationBuilder.DropTable(
                name: "WaitTimeRecords");
        }
    }
}
