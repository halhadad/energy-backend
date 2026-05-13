using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace energy_backend.Migrations
{
    /// <inheritdoc />
    public partial class EnergySummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EnergyReadings_DeviceId",
                table: "EnergyReadings");

            migrationBuilder.DropIndex(
                name: "IX_AggregatedEnergies_DeviceId",
                table: "AggregatedEnergies");

            migrationBuilder.CreateTable(
                name: "AlertEvents",
                columns: table => new
                {
                    AlertEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlertId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Threshold = table.Column<float>(type: "real", nullable: false),
                    TriggeredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertEvents", x => x.AlertEventId);
                    table.ForeignKey(
                        name: "FK_AlertEvents_Alerts_AlertId",
                        column: x => x.AlertId,
                        principalTable: "Alerts",
                        principalColumn: "AlertId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceConsumptionSummaries",
                columns: table => new
                {
                    DeviceConsumptionSummaryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    TotalConsumption = table.Column<double>(type: "float", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceConsumptionSummaries", x => x.DeviceConsumptionSummaryId);
                    table.ForeignKey(
                        name: "FK_DeviceConsumptionSummaries_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "DeviceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnergyReadings_DeviceId_Timestamp",
                table: "EnergyReadings",
                columns: new[] { "DeviceId", "Timestamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AggregatedEnergies_DeviceId_PeriodStartTime",
                table: "AggregatedEnergies",
                columns: new[] { "DeviceId", "PeriodStartTime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AlertEvents_AlertId",
                table: "AlertEvents",
                column: "AlertId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceConsumptionSummaries_DeviceId",
                table: "DeviceConsumptionSummaries",
                column: "DeviceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertEvents");

            migrationBuilder.DropTable(
                name: "DeviceConsumptionSummaries");

            migrationBuilder.DropIndex(
                name: "IX_EnergyReadings_DeviceId_Timestamp",
                table: "EnergyReadings");

            migrationBuilder.DropIndex(
                name: "IX_AggregatedEnergies_DeviceId_PeriodStartTime",
                table: "AggregatedEnergies");

            migrationBuilder.CreateIndex(
                name: "IX_EnergyReadings_DeviceId",
                table: "EnergyReadings",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_AggregatedEnergies_DeviceId",
                table: "AggregatedEnergies",
                column: "DeviceId");
        }
    }
}
