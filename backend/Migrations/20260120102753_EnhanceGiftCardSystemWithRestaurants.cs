using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnriGreifi.API.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceGiftCardSystemWithRestaurants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AmountAsText",
                table: "GiftCardTemplates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsMonetaryTemplate",
                table: "GiftCardTemplates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MessageTemplate",
                table: "GiftCardTemplates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RestaurantId",
                table: "GiftCardTemplates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RestaurantId",
                table: "GiftCards",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GiftCardTemplates_RestaurantId",
                table: "GiftCardTemplates",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_GiftCards_RestaurantId",
                table: "GiftCards",
                column: "RestaurantId");

            migrationBuilder.AddForeignKey(
                name: "FK_GiftCards_Restaurants_RestaurantId",
                table: "GiftCards",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_GiftCardTemplates_Restaurants_RestaurantId",
                table: "GiftCardTemplates",
                column: "RestaurantId",
                principalTable: "Restaurants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GiftCards_Restaurants_RestaurantId",
                table: "GiftCards");

            migrationBuilder.DropForeignKey(
                name: "FK_GiftCardTemplates_Restaurants_RestaurantId",
                table: "GiftCardTemplates");

            migrationBuilder.DropIndex(
                name: "IX_GiftCardTemplates_RestaurantId",
                table: "GiftCardTemplates");

            migrationBuilder.DropIndex(
                name: "IX_GiftCards_RestaurantId",
                table: "GiftCards");

            migrationBuilder.DropColumn(
                name: "AmountAsText",
                table: "GiftCardTemplates");

            migrationBuilder.DropColumn(
                name: "IsMonetaryTemplate",
                table: "GiftCardTemplates");

            migrationBuilder.DropColumn(
                name: "MessageTemplate",
                table: "GiftCardTemplates");

            migrationBuilder.DropColumn(
                name: "RestaurantId",
                table: "GiftCardTemplates");

            migrationBuilder.DropColumn(
                name: "RestaurantId",
                table: "GiftCards");
        }
    }
}
