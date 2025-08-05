using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace energy_backend.Migrations
{
    /// <inheritdoc />
    public partial class Aggregation1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AggregatedEnergies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalKwh = table.Column<float>(type: "real", nullable: false),
                    PeriodStartTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AggregatedEnergies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AggregatedEnergies_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "DeviceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AggregatedEnergies_DeviceId",
                table: "AggregatedEnergies",
                column: "DeviceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AggregatedEnergies");
        }
    }
}
