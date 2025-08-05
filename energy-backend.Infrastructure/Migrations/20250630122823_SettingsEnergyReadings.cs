using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace energy_backend.Migrations
{
    /// <inheritdoc />
    public partial class SettingsEnergyReadings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequireEmail",
                table: "Alerts");

            migrationBuilder.RenameColumn(
                name: "DarkMode",
                table: "Settings",
                newName: "UnusualAlerts");

            migrationBuilder.AddColumn<bool>(
                name: "BudgetAlerts",
                table: "Settings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "FavoriteOrg",
                table: "Settings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "PeakAlerts",
                table: "Settings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireEmail",
                table: "Settings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BudgetAlerts",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "FavoriteOrg",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "PeakAlerts",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "RequireEmail",
                table: "Settings");

            migrationBuilder.RenameColumn(
                name: "UnusualAlerts",
                table: "Settings",
                newName: "DarkMode");

            migrationBuilder.AddColumn<bool>(
                name: "RequireEmail",
                table: "Alerts",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
