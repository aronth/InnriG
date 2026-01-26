using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnriGreifi.API.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantEntityAndRestaurantIdToOrderRow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create Restaurants table first
            migrationBuilder.CreateTable(
                name: "Restaurants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Restaurants", x => x.Id);
                });

            // Seed restaurants with fixed GUIDs
            var greifinnId = new Guid("11111111-1111-1111-1111-111111111111");
            var spretturinnId = new Guid("22222222-2222-2222-2222-222222222222");
            var now = DateTime.UtcNow;

            migrationBuilder.InsertData(
                table: "Restaurants",
                columns: new[] { "Id", "Name", "Code", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { greifinnId, "Greifinn", "GRE", now, now },
                    { spretturinnId, "Spretturinn", "SPR", now, now }
                });

            migrationBuilder.DropIndex(
                name: "IX_WaitTimeNotifications_UserId_Restaurant",
                table: "WaitTimeNotifications");

            // Add RestaurantId columns (nullable for WaitTimeRecords and WaitTimeNotifications initially)
            migrationBuilder.AddColumn<Guid>(
                name: "RestaurantId",
                table: "WaitTimeRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RestaurantId",
                table: "WaitTimeNotifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RestaurantId",
                table: "OrderRows",
                type: "uuid",
                nullable: true);

            // Convert existing enum values to RestaurantId
            // Restaurant enum: 0 = Greifinn, 1 = Spretturinn
            migrationBuilder.Sql(@"
                UPDATE ""WaitTimeRecords""
                SET ""RestaurantId"" = '11111111-1111-1111-1111-111111111111'
                WHERE ""Restaurant"" = 0;

                UPDATE ""WaitTimeRecords""
                SET ""RestaurantId"" = '22222222-2222-2222-2222-222222222222'
                WHERE ""Restaurant"" = 1;

                UPDATE ""WaitTimeNotifications""
                SET ""RestaurantId"" = '11111111-1111-1111-1111-111111111111'
                WHERE ""Restaurant"" = 0;

                UPDATE ""WaitTimeNotifications""
                SET ""RestaurantId"" = '22222222-2222-2222-2222-222222222222'
                WHERE ""Restaurant"" = 1;
            ");

            // Now drop the old Restaurant enum columns
            migrationBuilder.DropColumn(
                name: "Restaurant",
                table: "WaitTimeRecords");

            migrationBuilder.DropColumn(
                name: "Restaurant",
                table: "WaitTimeNotifications");

            // Make RestaurantId non-nullable for WaitTimeRecords and WaitTimeNotifications
            migrationBuilder.AlterColumn<Guid>(
                name: "RestaurantId",
                table: "WaitTimeRecords",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "RestaurantId",
                table: "WaitTimeNotifications",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WaitTimeRecords_RestaurantId",
                table: "WaitTimeRecords",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_WaitTimeNotifications_RestaurantId",
                table: "WaitTimeNotifications",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_WaitTimeNotifications_UserId_RestaurantId",
                table: "WaitTimeNotifications",
                columns: new[] { "UserId", "RestaurantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderRows_RestaurantId",
                table: "OrderRows",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_Restaurants_Code",
                table: "Restaurants",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Restaurants_Name",
                table: "Restaurants",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderRows_Restaurants_RestaurantId",
                table: "OrderRows",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WaitTimeNotifications_Restaurants_RestaurantId",
                table: "WaitTimeNotifications",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WaitTimeRecords_Restaurants_RestaurantId",
                table: "WaitTimeRecords",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderRows_Restaurants_RestaurantId",
                table: "OrderRows");

            migrationBuilder.DropForeignKey(
                name: "FK_WaitTimeNotifications_Restaurants_RestaurantId",
                table: "WaitTimeNotifications");

            migrationBuilder.DropForeignKey(
                name: "FK_WaitTimeRecords_Restaurants_RestaurantId",
                table: "WaitTimeRecords");

            migrationBuilder.DropTable(
                name: "Restaurants");

            migrationBuilder.DropIndex(
                name: "IX_WaitTimeRecords_RestaurantId",
                table: "WaitTimeRecords");

            migrationBuilder.DropIndex(
                name: "IX_WaitTimeNotifications_RestaurantId",
                table: "WaitTimeNotifications");

            migrationBuilder.DropIndex(
                name: "IX_WaitTimeNotifications_UserId_RestaurantId",
                table: "WaitTimeNotifications");

            migrationBuilder.DropIndex(
                name: "IX_OrderRows_RestaurantId",
                table: "OrderRows");

            migrationBuilder.DropColumn(
                name: "RestaurantId",
                table: "WaitTimeRecords");

            migrationBuilder.DropColumn(
                name: "RestaurantId",
                table: "WaitTimeNotifications");

            migrationBuilder.DropColumn(
                name: "RestaurantId",
                table: "OrderRows");

            migrationBuilder.AddColumn<int>(
                name: "Restaurant",
                table: "WaitTimeRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Restaurant",
                table: "WaitTimeNotifications",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_WaitTimeNotifications_UserId_Restaurant",
                table: "WaitTimeNotifications",
                columns: new[] { "UserId", "Restaurant" },
                unique: true);
        }
    }
}
