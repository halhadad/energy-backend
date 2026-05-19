using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace energy_backend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class power : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EnergyValue",
                table: "EnergyReadings",
                newName: "PowerWatts");

            migrationBuilder.RenameColumn(
                name: "EnergyConsumption",
                table: "Devices",
                newName: "RatedPowerWatts");

            migrationBuilder.RenameColumn(
                name: "TotalEnergy",
                table: "AggregateMonthEnergies",
                newName: "TotalEnergyKwh");

            migrationBuilder.RenameColumn(
                name: "MinWatts",
                table: "AggregateMonthEnergies",
                newName: "MinPowerWatts");

            migrationBuilder.RenameColumn(
                name: "MaxWatts",
                table: "AggregateMonthEnergies",
                newName: "MaxPowerWatts");

            migrationBuilder.RenameColumn(
                name: "AverageWatts",
                table: "AggregateMonthEnergies",
                newName: "AveragePowerWatts");

            migrationBuilder.RenameColumn(
                name: "TotalEnergy",
                table: "AggregateMinuteEnergies",
                newName: "TotalEnergyKwh");

            migrationBuilder.RenameColumn(
                name: "MinWatts",
                table: "AggregateMinuteEnergies",
                newName: "MinPowerWatts");

            migrationBuilder.RenameColumn(
                name: "MaxWatts",
                table: "AggregateMinuteEnergies",
                newName: "MaxPowerWatts");

            migrationBuilder.RenameColumn(
                name: "AverageWatts",
                table: "AggregateMinuteEnergies",
                newName: "AveragePowerWatts");

            migrationBuilder.RenameColumn(
                name: "TotalEnergy",
                table: "AggregateHourEnergies",
                newName: "TotalEnergyKwh");

            migrationBuilder.RenameColumn(
                name: "MinWatts",
                table: "AggregateHourEnergies",
                newName: "MinPowerWatts");

            migrationBuilder.RenameColumn(
                name: "MaxWatts",
                table: "AggregateHourEnergies",
                newName: "MaxPowerWatts");

            migrationBuilder.RenameColumn(
                name: "AverageWatts",
                table: "AggregateHourEnergies",
                newName: "AveragePowerWatts");

            migrationBuilder.RenameColumn(
                name: "TotalEnergy",
                table: "AggregateDayEnergies",
                newName: "TotalEnergyKwh");

            migrationBuilder.RenameColumn(
                name: "MinWatts",
                table: "AggregateDayEnergies",
                newName: "MinPowerWatts");

            migrationBuilder.RenameColumn(
                name: "MaxWatts",
                table: "AggregateDayEnergies",
                newName: "MaxPowerWatts");

            migrationBuilder.RenameColumn(
                name: "AverageWatts",
                table: "AggregateDayEnergies",
                newName: "AveragePowerWatts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PowerWatts",
                table: "EnergyReadings",
                newName: "EnergyValue");

            migrationBuilder.RenameColumn(
                name: "RatedPowerWatts",
                table: "Devices",
                newName: "EnergyConsumption");

            migrationBuilder.RenameColumn(
                name: "TotalEnergyKwh",
                table: "AggregateMonthEnergies",
                newName: "TotalEnergy");

            migrationBuilder.RenameColumn(
                name: "MinPowerWatts",
                table: "AggregateMonthEnergies",
                newName: "MinWatts");

            migrationBuilder.RenameColumn(
                name: "MaxPowerWatts",
                table: "AggregateMonthEnergies",
                newName: "MaxWatts");

            migrationBuilder.RenameColumn(
                name: "AveragePowerWatts",
                table: "AggregateMonthEnergies",
                newName: "AverageWatts");

            migrationBuilder.RenameColumn(
                name: "TotalEnergyKwh",
                table: "AggregateMinuteEnergies",
                newName: "TotalEnergy");

            migrationBuilder.RenameColumn(
                name: "MinPowerWatts",
                table: "AggregateMinuteEnergies",
                newName: "MinWatts");

            migrationBuilder.RenameColumn(
                name: "MaxPowerWatts",
                table: "AggregateMinuteEnergies",
                newName: "MaxWatts");

            migrationBuilder.RenameColumn(
                name: "AveragePowerWatts",
                table: "AggregateMinuteEnergies",
                newName: "AverageWatts");

            migrationBuilder.RenameColumn(
                name: "TotalEnergyKwh",
                table: "AggregateHourEnergies",
                newName: "TotalEnergy");

            migrationBuilder.RenameColumn(
                name: "MinPowerWatts",
                table: "AggregateHourEnergies",
                newName: "MinWatts");

            migrationBuilder.RenameColumn(
                name: "MaxPowerWatts",
                table: "AggregateHourEnergies",
                newName: "MaxWatts");

            migrationBuilder.RenameColumn(
                name: "AveragePowerWatts",
                table: "AggregateHourEnergies",
                newName: "AverageWatts");

            migrationBuilder.RenameColumn(
                name: "TotalEnergyKwh",
                table: "AggregateDayEnergies",
                newName: "TotalEnergy");

            migrationBuilder.RenameColumn(
                name: "MinPowerWatts",
                table: "AggregateDayEnergies",
                newName: "MinWatts");

            migrationBuilder.RenameColumn(
                name: "MaxPowerWatts",
                table: "AggregateDayEnergies",
                newName: "MaxWatts");

            migrationBuilder.RenameColumn(
                name: "AveragePowerWatts",
                table: "AggregateDayEnergies",
                newName: "AverageWatts");
        }
    }
}
