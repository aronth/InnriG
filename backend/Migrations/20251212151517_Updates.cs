using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnriGreifi.API.Migrations
{
    /// <inheritdoc />
    public partial class Updates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ListPrice",
                table: "InvoiceItems",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "VatCode",
                table: "InvoiceItems",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ListPrice",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "VatCode",
                table: "InvoiceItems");
        }
    }
}
