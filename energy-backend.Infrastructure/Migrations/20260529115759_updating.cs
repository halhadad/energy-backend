using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace energy_backend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Budget",
                table: "Organisations",
                newName: "EnergyBudgetKwh");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EnergyBudgetKwh",
                table: "Organisations",
                newName: "Budget");
        }
    }
}
