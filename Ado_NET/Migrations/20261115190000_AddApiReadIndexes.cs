using Kpz.Lab2.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kpz.Lab2.Migrations;

[DbContext(typeof(KpzDbContext))]
[Migration("20261115190000_AddApiReadIndexes")]
public partial class AddApiReadIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_Trips_OwnerId_StartDate",
            table: "Trips",
            columns: new[] { "OwnerId", "StartDate" });

        migrationBuilder.CreateIndex(
            name: "IX_TripMembers_TripId_Role",
            table: "TripMembers",
            columns: new[] { "TripId", "Role" });

        migrationBuilder.CreateIndex(
            name: "IX_TripMembers_TripId_JoinedAt",
            table: "TripMembers",
            columns: new[] { "TripId", "JoinedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_ItineraryItems_TripId_EndDateTime",
            table: "ItineraryItems",
            columns: new[] { "TripId", "EndDateTime" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Trips_OwnerId_StartDate", table: "Trips");
        migrationBuilder.DropIndex(name: "IX_TripMembers_TripId_Role", table: "TripMembers");
        migrationBuilder.DropIndex(name: "IX_TripMembers_TripId_JoinedAt", table: "TripMembers");
        migrationBuilder.DropIndex(name: "IX_ItineraryItems_TripId_EndDateTime", table: "ItineraryItems");
    }
}
