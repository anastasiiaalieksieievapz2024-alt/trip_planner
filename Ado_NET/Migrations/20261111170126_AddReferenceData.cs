using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Kpz.Lab2.Migrations
{
    /// <inheritdoc />
    public partial class AddReferenceData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ItemType",
                table: "ItineraryItems");

            migrationBuilder.AddColumn<int>(
                name: "TripCategoryId",
                table: "Trips",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ItineraryItemTypeId",
                table: "ItineraryItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ItineraryItemTypes",
                columns: table => new
                {
                    ItineraryItemTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItineraryItemTypes", x => x.ItineraryItemTypeId);
                });

            migrationBuilder.CreateTable(
                name: "TripCategories",
                columns: table => new
                {
                    TripCategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripCategories", x => x.TripCategoryId);
                });

            migrationBuilder.InsertData(
                table: "ItineraryItemTypes",
                columns: new[] { "ItineraryItemTypeId", "Name" },
                values: new object[,]
                {
                    { 1, "Transport" },
                    { 2, "Accommodation" },
                    { 3, "Transfer" },
                    { 4, "Activity" },
                    { 5, "Meal" }
                });

            migrationBuilder.InsertData(
                table: "TripCategories",
                columns: new[] { "TripCategoryId", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Коротка подорож до міста", "City break" },
                    { 2, "Відпочинок біля моря або озера", "Beach" },
                    { 3, "Активна подорож і пригоди", "Adventure" },
                    { 4, "Музеї, пам'ятки та культурні події", "Cultural" },
                    { 5, "Подорож для сімейного відпочинку", "Family" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trips_TripCategoryId",
                table: "Trips",
                column: "TripCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ItineraryItems_ItineraryItemTypeId",
                table: "ItineraryItems",
                column: "ItineraryItemTypeId");

            migrationBuilder.CreateIndex(
                name: "UX_ItineraryItemTypes_Name",
                table: "ItineraryItemTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_TripCategories_Name",
                table: "TripCategories",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ItineraryItems_ItineraryItemTypes_ItineraryItemTypeId",
                table: "ItineraryItems",
                column: "ItineraryItemTypeId",
                principalTable: "ItineraryItemTypes",
                principalColumn: "ItineraryItemTypeId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_TripCategories_TripCategoryId",
                table: "Trips",
                column: "TripCategoryId",
                principalTable: "TripCategories",
                principalColumn: "TripCategoryId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItineraryItems_ItineraryItemTypes_ItineraryItemTypeId",
                table: "ItineraryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Trips_TripCategories_TripCategoryId",
                table: "Trips");

            migrationBuilder.DropTable(
                name: "ItineraryItemTypes");

            migrationBuilder.DropTable(
                name: "TripCategories");

            migrationBuilder.DropIndex(
                name: "IX_Trips_TripCategoryId",
                table: "Trips");

            migrationBuilder.DropIndex(
                name: "IX_ItineraryItems_ItineraryItemTypeId",
                table: "ItineraryItems");

            migrationBuilder.DropColumn(
                name: "TripCategoryId",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "ItineraryItemTypeId",
                table: "ItineraryItems");

            migrationBuilder.AddColumn<string>(
                name: "ItemType",
                table: "ItineraryItems",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");
        }
    }
}
