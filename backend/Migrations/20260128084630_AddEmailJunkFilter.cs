using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnriGreifi.API.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailJunkFilter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailJunkFilters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SenderEmail = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailJunkFilters", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailJunkFilters_IsActive",
                table: "EmailJunkFilters",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_EmailJunkFilters_Subject_SenderEmail",
                table: "EmailJunkFilters",
                columns: new[] { "Subject", "SenderEmail" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailJunkFilters");
        }
    }
}
