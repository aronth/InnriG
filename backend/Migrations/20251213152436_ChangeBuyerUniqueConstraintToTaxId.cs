using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnriGreifi.API.Migrations
{
    /// <inheritdoc />
    public partial class ChangeBuyerUniqueConstraintToTaxId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Buyers_Name",
                table: "Buyers");

            migrationBuilder.AddColumn<string>(
                name: "BuyerTaxId",
                table: "Invoices",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "TaxId",
                table: "Buyers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Buyers_TaxId",
                table: "Buyers",
                column: "TaxId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Buyers_TaxId",
                table: "Buyers");

            migrationBuilder.DropColumn(
                name: "BuyerTaxId",
                table: "Invoices");

            migrationBuilder.AlterColumn<string>(
                name: "TaxId",
                table: "Buyers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateIndex(
                name: "IX_Buyers_Name",
                table: "Buyers",
                column: "Name",
                unique: true);
        }
    }
}
