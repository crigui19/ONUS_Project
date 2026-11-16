using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Test_ONUS.Migrations
{
    /// <inheritdoc />
    public partial class AddDataProtectionKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ValoriSessione_SessioneId",
                table: "ValoriSessione");

            migrationBuilder.DropIndex(
                name: "IX_SottoscrizioniPush_AtletaId",
                table: "SottoscrizioniPush");

            migrationBuilder.AlterColumn<string>(
                name: "Password",
                table: "PreparatoriAtletici",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "GoogleId",
                table: "PreparatoriAtletici",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ValoriSessione_SessioneId_ParametroId",
                table: "ValoriSessione",
                columns: new[] { "SessioneId", "ParametroId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SottoscrizioniPush_AtletaId_Endpoint",
                table: "SottoscrizioniPush",
                columns: new[] { "AtletaId", "Endpoint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sessioni_AtletaId_Data",
                table: "Sessioni",
                columns: new[] { "AtletaId", "Data" });

            migrationBuilder.CreateIndex(
                name: "IX_PreparatoriAtletici_Email",
                table: "PreparatoriAtletici",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Parametri_SquadraId",
                table: "Parametri",
                column: "SquadraId");

            migrationBuilder.AddForeignKey(
                name: "FK_Parametri_Squadre_SquadraId",
                table: "Parametri",
                column: "SquadraId",
                principalTable: "Squadre",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Sessioni_Atleti_AtletaId",
                table: "Sessioni",
                column: "AtletaId",
                principalTable: "Atleti",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Parametri_Squadre_SquadraId",
                table: "Parametri");

            migrationBuilder.DropForeignKey(
                name: "FK_Sessioni_Atleti_AtletaId",
                table: "Sessioni");

            migrationBuilder.DropIndex(
                name: "IX_ValoriSessione_SessioneId_ParametroId",
                table: "ValoriSessione");

            migrationBuilder.DropIndex(
                name: "IX_SottoscrizioniPush_AtletaId_Endpoint",
                table: "SottoscrizioniPush");

            migrationBuilder.DropIndex(
                name: "IX_Sessioni_AtletaId_Data",
                table: "Sessioni");

            migrationBuilder.DropIndex(
                name: "IX_PreparatoriAtletici_Email",
                table: "PreparatoriAtletici");

            migrationBuilder.DropIndex(
                name: "IX_Parametri_SquadraId",
                table: "Parametri");

            migrationBuilder.DropColumn(
                name: "GoogleId",
                table: "PreparatoriAtletici");

            migrationBuilder.AlterColumn<string>(
                name: "Password",
                table: "PreparatoriAtletici",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ValoriSessione_SessioneId",
                table: "ValoriSessione",
                column: "SessioneId");

            migrationBuilder.CreateIndex(
                name: "IX_SottoscrizioniPush_AtletaId",
                table: "SottoscrizioniPush",
                column: "AtletaId");
        }
    }
}
