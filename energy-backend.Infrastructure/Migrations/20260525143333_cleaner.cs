using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace energy_backend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class cleaner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefreshTokenExpiryTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Organisations",
                columns: table => new
                {
                    OrganisationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Budget = table.Column<float>(type: "real", nullable: false),
                    EnergyCostPerKwh = table.Column<float>(type: "real", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organisations", x => x.OrganisationId);
                    table.ForeignKey(
                        name: "FK_Organisations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    SettingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FavoriteOrg = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeakAlerts = table.Column<bool>(type: "bit", nullable: false),
                    UnusualAlerts = table.Column<bool>(type: "bit", nullable: false),
                    BudgetAlerts = table.Column<bool>(type: "bit", nullable: false),
                    RequireEmail = table.Column<bool>(type: "bit", nullable: false),
                    ElectricityCostPerKwh = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.SettingId);
                    table.ForeignKey(
                        name: "FK_Settings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Alerts",
                columns: table => new
                {
                    AlertId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ThresholdValue = table.Column<float>(type: "real", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "bit", nullable: false),
                    InAppNotificationEnabled = table.Column<bool>(type: "bit", nullable: false),
                    InAppCooldownMinutes = table.Column<float>(type: "real", nullable: false),
                    EmailCooldownMinutes = table.Column<float>(type: "real", nullable: false),
                    LastTriggeredInAppAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastTriggeredEmailAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerts", x => x.AlertId);
                    table.ForeignKey(
                        name: "FK_Alerts_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "OrganisationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RatedPowerWatts = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.DeviceId);
                    table.ForeignKey(
                        name: "FK_Devices_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "OrganisationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlertEvents",
                columns: table => new
                {
                    AlertEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlertId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ThresholdValue = table.Column<float>(type: "real", nullable: false),
                    TriggeredValueWatts = table.Column<float>(type: "real", nullable: false),
                    TriggeredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsAcknowledged = table.Column<bool>(type: "bit", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcknowledgedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                name: "AggregateDayEnergies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalActiveEnergyKwh = table.Column<float>(type: "real", nullable: false),
                    AverageActivePowerWatts = table.Column<float>(type: "real", nullable: false),
                    MinActivePowerWatts = table.Column<float>(type: "real", nullable: false),
                    MaxActivePowerWatts = table.Column<float>(type: "real", nullable: false),
                    AverageVoltageVolts = table.Column<float>(type: "real", nullable: false),
                    AverageCurrentAmps = table.Column<float>(type: "real", nullable: false),
                    AveragePowerFactor = table.Column<float>(type: "real", nullable: false),
                    DataPointsCount = table.Column<int>(type: "int", nullable: false),
                    EstimatedCost = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AggregateDayEnergies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AggregateDayEnergies_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "DeviceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AggregateDayEnergies_Organisations_OrgId",
                        column: x => x.OrgId,
                        principalTable: "Organisations",
                        principalColumn: "OrganisationId");
                });

            migrationBuilder.CreateTable(
                name: "AggregateHourEnergies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalActiveEnergyKwh = table.Column<float>(type: "real", nullable: false),
                    AverageActivePowerWatts = table.Column<float>(type: "real", nullable: false),
                    MinActivePowerWatts = table.Column<float>(type: "real", nullable: false),
                    MaxActivePowerWatts = table.Column<float>(type: "real", nullable: false),
                    AverageVoltageVolts = table.Column<float>(type: "real", nullable: false),
                    AverageCurrentAmps = table.Column<float>(type: "real", nullable: false),
                    AveragePowerFactor = table.Column<float>(type: "real", nullable: false),
                    DataPointsCount = table.Column<int>(type: "int", nullable: false),
                    EstimatedCost = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AggregateHourEnergies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AggregateHourEnergies_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "DeviceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AggregateHourEnergies_Organisations_OrgId",
                        column: x => x.OrgId,
                        principalTable: "Organisations",
                        principalColumn: "OrganisationId");
                });

            migrationBuilder.CreateTable(
                name: "AggregateMinuteEnergies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalActiveEnergyKwh = table.Column<float>(type: "real", nullable: false),
                    AverageActivePowerWatts = table.Column<float>(type: "real", nullable: false),
                    MinActivePowerWatts = table.Column<float>(type: "real", nullable: false),
                    MaxActivePowerWatts = table.Column<float>(type: "real", nullable: false),
                    AverageVoltageVolts = table.Column<float>(type: "real", nullable: false),
                    AverageCurrentAmps = table.Column<float>(type: "real", nullable: false),
                    AveragePowerFactor = table.Column<float>(type: "real", nullable: false),
                    DataPointsCount = table.Column<int>(type: "int", nullable: false),
                    EstimatedCost = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AggregateMinuteEnergies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AggregateMinuteEnergies_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "DeviceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AggregateMinuteEnergies_Organisations_OrgId",
                        column: x => x.OrgId,
                        principalTable: "Organisations",
                        principalColumn: "OrganisationId");
                });

            migrationBuilder.CreateTable(
                name: "AggregateMonthEnergies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalActiveEnergyKwh = table.Column<float>(type: "real", nullable: false),
                    AverageActivePowerWatts = table.Column<float>(type: "real", nullable: false),
                    MinActivePowerWatts = table.Column<float>(type: "real", nullable: false),
                    MaxActivePowerWatts = table.Column<float>(type: "real", nullable: false),
                    AverageVoltageVolts = table.Column<float>(type: "real", nullable: false),
                    AverageCurrentAmps = table.Column<float>(type: "real", nullable: false),
                    AveragePowerFactor = table.Column<float>(type: "real", nullable: false),
                    DataPointsCount = table.Column<int>(type: "int", nullable: false),
                    EstimatedCost = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AggregateMonthEnergies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AggregateMonthEnergies_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "DeviceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AggregateMonthEnergies_Organisations_OrgId",
                        column: x => x.OrgId,
                        principalTable: "Organisations",
                        principalColumn: "OrganisationId");
                });

            migrationBuilder.CreateTable(
                name: "EnergyReadings",
                columns: table => new
                {
                    EnergyReadingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActivePowerWatts = table.Column<float>(type: "real", nullable: false),
                    VoltageVolts = table.Column<float>(type: "real", nullable: false),
                    CurrentAmps = table.Column<float>(type: "real", nullable: false),
                    PowerFactor = table.Column<float>(type: "real", nullable: false),
                    ActiveEnergyKwh = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnergyReadings", x => x.EnergyReadingId);
                    table.ForeignKey(
                        name: "FK_EnergyReadings_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "DeviceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AggregateDayEnergies_DeviceId_Timestamp",
                table: "AggregateDayEnergies",
                columns: new[] { "DeviceId", "Timestamp" },
                unique: true,
                filter: "[DeviceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AggregateDayEnergies_OrgId_Timestamp",
                table: "AggregateDayEnergies",
                columns: new[] { "OrgId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AggregateHourEnergies_DeviceId_Timestamp",
                table: "AggregateHourEnergies",
                columns: new[] { "DeviceId", "Timestamp" },
                unique: true,
                filter: "[DeviceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AggregateHourEnergies_OrgId_Timestamp",
                table: "AggregateHourEnergies",
                columns: new[] { "OrgId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AggregateMinuteEnergies_DeviceId_Timestamp",
                table: "AggregateMinuteEnergies",
                columns: new[] { "DeviceId", "Timestamp" },
                unique: true,
                filter: "[DeviceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AggregateMinuteEnergies_OrgId_Timestamp",
                table: "AggregateMinuteEnergies",
                columns: new[] { "OrgId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AggregateMonthEnergies_DeviceId_Timestamp",
                table: "AggregateMonthEnergies",
                columns: new[] { "DeviceId", "Timestamp" },
                unique: true,
                filter: "[DeviceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AggregateMonthEnergies_OrgId_Timestamp",
                table: "AggregateMonthEnergies",
                columns: new[] { "OrgId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AlertEvents_AlertId",
                table: "AlertEvents",
                column: "AlertId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_OrganisationId",
                table: "Alerts",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_OrganisationId",
                table: "Devices",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_EnergyReadings_DeviceId_Timestamp",
                table: "EnergyReadings",
                columns: new[] { "DeviceId", "Timestamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnergyReadings_Timestamp_DeviceId",
                table: "EnergyReadings",
                columns: new[] { "Timestamp", "DeviceId" });

            migrationBuilder.CreateIndex(
                name: "IX_Organisations_UserId",
                table: "Organisations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Settings_UserId",
                table: "Settings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AggregateDayEnergies");

            migrationBuilder.DropTable(
                name: "AggregateHourEnergies");

            migrationBuilder.DropTable(
                name: "AggregateMinuteEnergies");

            migrationBuilder.DropTable(
                name: "AggregateMonthEnergies");

            migrationBuilder.DropTable(
                name: "AlertEvents");

            migrationBuilder.DropTable(
                name: "EnergyReadings");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "Alerts");

            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropTable(
                name: "Organisations");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
