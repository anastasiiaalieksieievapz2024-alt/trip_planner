using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kpz.Lab2.Migrations
{
    /// <inheritdoc />
    public partial class AddTripPlanningNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlanningNotes",
                table: "Trips",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlanningNotes",
                table: "Trips");
        }
    }
}
