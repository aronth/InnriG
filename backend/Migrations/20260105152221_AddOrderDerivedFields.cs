using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnriGreifi.API.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderDerivedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveryMethod",
                table: "OrderRows",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<string>(
                name: "OrderSource",
                table: "OrderRows",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "OrderTime",
                table: "OrderRows",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "ReadyTime",
                table: "OrderRows",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WaitTimeMin",
                table: "OrderRows",
                type: "integer",
                nullable: true);

            // Backfill any pre-existing rows created before these columns existed
            migrationBuilder.Sql("""
                UPDATE "OrderRows"
                SET "DeliveryMethod" = 'Unknown'
                WHERE "DeliveryMethod" IS NULL OR "DeliveryMethod" = '';

                UPDATE "OrderRows"
                SET "OrderSource" = 'Unknown'
                WHERE "OrderSource" IS NULL OR "OrderSource" = '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryMethod",
                table: "OrderRows");

            migrationBuilder.DropColumn(
                name: "OrderSource",
                table: "OrderRows");

            migrationBuilder.DropColumn(
                name: "OrderTime",
                table: "OrderRows");

            migrationBuilder.DropColumn(
                name: "ReadyTime",
                table: "OrderRows");

            migrationBuilder.DropColumn(
                name: "WaitTimeMin",
                table: "OrderRows");
        }
    }
}
