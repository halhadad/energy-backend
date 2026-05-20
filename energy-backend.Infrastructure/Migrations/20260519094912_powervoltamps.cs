using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace energy_backend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class powervoltamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PowerWatts",
                table: "EnergyReadings",
                newName: "VoltageVolts");

            migrationBuilder.RenameColumn(
                name: "MinPowerWatts",
                table: "AggregateMonthEnergies",
                newName: "MinActivePowerWatts");

            migrationBuilder.RenameColumn(
                name: "MaxPowerWatts",
                table: "AggregateMonthEnergies",
                newName: "MaxActivePowerWatts");

            migrationBuilder.RenameColumn(
                name: "AveragePowerWatts",
                table: "AggregateMonthEnergies",
                newName: "AverageVoltageVolts");

            migrationBuilder.RenameColumn(
                name: "MinPowerWatts",
                table: "AggregateMinuteEnergies",
                newName: "MinActivePowerWatts");

            migrationBuilder.RenameColumn(
                name: "MaxPowerWatts",
                table: "AggregateMinuteEnergies",
                newName: "MaxActivePowerWatts");

            migrationBuilder.RenameColumn(
                name: "AveragePowerWatts",
                table: "AggregateMinuteEnergies",
                newName: "AverageVoltageVolts");

            migrationBuilder.RenameColumn(
                name: "MinPowerWatts",
                table: "AggregateHourEnergies",
                newName: "MinActivePowerWatts");

            migrationBuilder.RenameColumn(
                name: "MaxPowerWatts",
                table: "AggregateHourEnergies",
                newName: "MaxActivePowerWatts");

            migrationBuilder.RenameColumn(
                name: "AveragePowerWatts",
                table: "AggregateHourEnergies",
                newName: "AverageVoltageVolts");

            migrationBuilder.RenameColumn(
                name: "MinPowerWatts",
                table: "AggregateDayEnergies",
                newName: "MinActivePowerWatts");

            migrationBuilder.RenameColumn(
                name: "MaxPowerWatts",
                table: "AggregateDayEnergies",
                newName: "MaxActivePowerWatts");

            migrationBuilder.RenameColumn(
                name: "AveragePowerWatts",
                table: "AggregateDayEnergies",
                newName: "AverageVoltageVolts");

            migrationBuilder.AddColumn<float>(
                name: "ElectricityCostPerKwh",
                table: "Settings",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "ActivePowerWatts",
                table: "EnergyReadings",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "CurrentAmps",
                table: "EnergyReadings",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "PowerFactor",
                table: "EnergyReadings",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "AverageActivePowerWatts",
                table: "AggregateMonthEnergies",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "AverageCurrentAmps",
                table: "AggregateMonthEnergies",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "AveragePowerFactor",
                table: "AggregateMonthEnergies",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "AverageActivePowerWatts",
                table: "AggregateMinuteEnergies",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "AverageCurrentAmps",
                table: "AggregateMinuteEnergies",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "AveragePowerFactor",
                table: "AggregateMinuteEnergies",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "AverageActivePowerWatts",
                table: "AggregateHourEnergies",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "AverageCurrentAmps",
                table: "AggregateHourEnergies",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "AveragePowerFactor",
                table: "AggregateHourEnergies",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "AverageActivePowerWatts",
                table: "AggregateDayEnergies",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "AverageCurrentAmps",
                table: "AggregateDayEnergies",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "AveragePowerFactor",
                table: "AggregateDayEnergies",
                type: "real",
                nullable: false,
                defaultValue: 0f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ElectricityCostPerKwh",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "ActivePowerWatts",
                table: "EnergyReadings");

            migrationBuilder.DropColumn(
                name: "CurrentAmps",
                table: "EnergyReadings");

            migrationBuilder.DropColumn(
                name: "PowerFactor",
                table: "EnergyReadings");

            migrationBuilder.DropColumn(
                name: "AverageActivePowerWatts",
                table: "AggregateMonthEnergies");

            migrationBuilder.DropColumn(
                name: "AverageCurrentAmps",
                table: "AggregateMonthEnergies");

            migrationBuilder.DropColumn(
                name: "AveragePowerFactor",
                table: "AggregateMonthEnergies");

            migrationBuilder.DropColumn(
                name: "AverageActivePowerWatts",
                table: "AggregateMinuteEnergies");

            migrationBuilder.DropColumn(
                name: "AverageCurrentAmps",
                table: "AggregateMinuteEnergies");

            migrationBuilder.DropColumn(
                name: "AveragePowerFactor",
                table: "AggregateMinuteEnergies");

            migrationBuilder.DropColumn(
                name: "AverageActivePowerWatts",
                table: "AggregateHourEnergies");

            migrationBuilder.DropColumn(
                name: "AverageCurrentAmps",
                table: "AggregateHourEnergies");

            migrationBuilder.DropColumn(
                name: "AveragePowerFactor",
                table: "AggregateHourEnergies");

            migrationBuilder.DropColumn(
                name: "AverageActivePowerWatts",
                table: "AggregateDayEnergies");

            migrationBuilder.DropColumn(
                name: "AverageCurrentAmps",
                table: "AggregateDayEnergies");

            migrationBuilder.DropColumn(
                name: "AveragePowerFactor",
                table: "AggregateDayEnergies");

            migrationBuilder.RenameColumn(
                name: "VoltageVolts",
                table: "EnergyReadings",
                newName: "PowerWatts");

            migrationBuilder.RenameColumn(
                name: "MinActivePowerWatts",
                table: "AggregateMonthEnergies",
                newName: "MinPowerWatts");

            migrationBuilder.RenameColumn(
                name: "MaxActivePowerWatts",
                table: "AggregateMonthEnergies",
                newName: "MaxPowerWatts");

            migrationBuilder.RenameColumn(
                name: "AverageVoltageVolts",
                table: "AggregateMonthEnergies",
                newName: "AveragePowerWatts");

            migrationBuilder.RenameColumn(
                name: "MinActivePowerWatts",
                table: "AggregateMinuteEnergies",
                newName: "MinPowerWatts");

            migrationBuilder.RenameColumn(
                name: "MaxActivePowerWatts",
                table: "AggregateMinuteEnergies",
                newName: "MaxPowerWatts");

            migrationBuilder.RenameColumn(
                name: "AverageVoltageVolts",
                table: "AggregateMinuteEnergies",
                newName: "AveragePowerWatts");

            migrationBuilder.RenameColumn(
                name: "MinActivePowerWatts",
                table: "AggregateHourEnergies",
                newName: "MinPowerWatts");

            migrationBuilder.RenameColumn(
                name: "MaxActivePowerWatts",
                table: "AggregateHourEnergies",
                newName: "MaxPowerWatts");

            migrationBuilder.RenameColumn(
                name: "AverageVoltageVolts",
                table: "AggregateHourEnergies",
                newName: "AveragePowerWatts");

            migrationBuilder.RenameColumn(
                name: "MinActivePowerWatts",
                table: "AggregateDayEnergies",
                newName: "MinPowerWatts");

            migrationBuilder.RenameColumn(
                name: "MaxActivePowerWatts",
                table: "AggregateDayEnergies",
                newName: "MaxPowerWatts");

            migrationBuilder.RenameColumn(
                name: "AverageVoltageVolts",
                table: "AggregateDayEnergies",
                newName: "AveragePowerWatts");
        }
    }
}
