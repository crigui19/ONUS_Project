using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Test_ONUS.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSessionDurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DurataMinuti",
                table: "Sessioni",
                newName: "TempoEffettivoMinuti");

            migrationBuilder.AddColumn<int>(
                name: "DurataTotaleMinuti",
                table: "Sessioni",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DurataTotaleMinuti",
                table: "Sessioni");

            migrationBuilder.RenameColumn(
                name: "TempoEffettivoMinuti",
                table: "Sessioni",
                newName: "DurataMinuti");
        }
    }
}
