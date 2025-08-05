using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace energy_backend.Migrations
{
    /// <inheritdoc />
    public partial class seeding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Energies",
                columns: new[] { "id", "CurrentConsumption", "TotalConsumption" },
                values: new object[] { 1, 150.5f, 1200.75f });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Energies",
                keyColumn: "id",
                keyValue: 1);
        }
    }
}
