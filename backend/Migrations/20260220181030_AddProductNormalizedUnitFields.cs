using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnriGreifi.API.Migrations
{
    /// <inheritdoc />
    public partial class AddProductNormalizedUnitFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalizedBaseUnit",
                table: "Products",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NormalizedUnitMultiplier",
                table: "Products",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NormalizedBaseUnit",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "NormalizedUnitMultiplier",
                table: "Products");
        }
    }
}
