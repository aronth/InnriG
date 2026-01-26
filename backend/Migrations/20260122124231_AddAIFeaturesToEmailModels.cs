using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnriGreifi.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAIFeaturesToEmailModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AISummary",
                table: "EmailMessages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAIResponse",
                table: "EmailMessages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MessageBody",
                table: "EmailMessages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactName",
                table: "EmailExtractedData",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AISummary",
                table: "EmailMessages");

            migrationBuilder.DropColumn(
                name: "IsAIResponse",
                table: "EmailMessages");

            migrationBuilder.DropColumn(
                name: "MessageBody",
                table: "EmailMessages");

            migrationBuilder.DropColumn(
                name: "ContactName",
                table: "EmailExtractedData");
        }
    }
}
