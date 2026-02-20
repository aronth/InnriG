using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnriGreifi.API.Migrations
{
    /// <inheritdoc />
    public partial class AddUserEmailToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserEmailTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailAddress = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    EncryptedRefreshToken = table.Column<string>(type: "text", nullable: false),
                    AccessToken = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastRefreshedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsSystemInbox = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEmailTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserEmailTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserEmailTokens_IsSystemInbox",
                table: "UserEmailTokens",
                column: "IsSystemInbox");

            migrationBuilder.CreateIndex(
                name: "IX_UserEmailTokens_UserId",
                table: "UserEmailTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserEmailTokens_UserId_EmailAddress",
                table: "UserEmailTokens",
                columns: new[] { "UserId", "EmailAddress" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserEmailTokens");
        }
    }
}
