using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace energy_backend.Migrations
{
    /// <inheritdoc />
    public partial class AlertsChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Alerts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastTriggeredAt",
                table: "Alerts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganisationId",
                table: "AlertEvents",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<float>(
                name: "TriggeredEnergy",
                table: "AlertEvents",
                type: "real",
                nullable: false,
                defaultValue: 0f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "LastTriggeredAt",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "OrganisationId",
                table: "AlertEvents");

            migrationBuilder.DropColumn(
                name: "TriggeredEnergy",
                table: "AlertEvents");
        }
    }
}
