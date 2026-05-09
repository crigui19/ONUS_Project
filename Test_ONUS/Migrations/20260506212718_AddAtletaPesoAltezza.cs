using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Test_ONUS.Migrations
{
    /// <inheritdoc />
    public partial class AddAtletaPesoAltezza : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Altezza",
                table: "Atleti",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "Peso",
                table: "Atleti",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Altezza",
                table: "Atleti");

            migrationBuilder.DropColumn(
                name: "Peso",
                table: "Atleti");
        }
    }
}
