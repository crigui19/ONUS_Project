using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Test_ONUS.Migrations
{
    /// <inheritdoc />
    public partial class AddResetToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sessioni_Atleti_AtletaId",
                table: "Sessioni");

            migrationBuilder.DropIndex(
                name: "IX_Sessioni_AtletaId",
                table: "Sessioni");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Preparatori",
                table: "Preparatori");

            migrationBuilder.DropColumn(
                name: "TipologiaValore",
                table: "Parametri");

            migrationBuilder.RenameTable(
                name: "Preparatori",
                newName: "PreparatoriAtletici");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "PreparatoriAtletici",
                newName: "Password");

            migrationBuilder.AlterColumn<int>(
                name: "Valore",
                table: "ValoriSessione",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Data",
                table: "Sessioni",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "DurataTotaleMinuti",
                table: "Sessioni",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Sessioni",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TempoEffettivoMinuti",
                table: "Sessioni",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsAttivo",
                table: "Parametri",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsCalcoloCarico",
                table: "Parametri",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SquadraId",
                table: "Parametri",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ValoreMassimo",
                table: "Parametri",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ValoreMinimo",
                table: "Parametri",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Altezza",
                table: "Atleti",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DescrizioneInfortunio",
                table: "Atleti",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FotoUrl",
                table: "Atleti",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsAttivo",
                table: "Atleti",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsInRiabilitazione",
                table: "Atleti",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsInfortunato",
                table: "Atleti",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "Atleti",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Peso",
                table: "Atleti",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "ResetToken",
                table: "PreparatoriAtletici",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResetTokenScadenza",
                table: "PreparatoriAtletici",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SquadraId",
                table: "PreparatoriAtletici",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PreparatoriAtletici",
                table: "PreparatoriAtletici",
                column: "Id");

            migrationBuilder.InsertData(
                table: "Parametri",
                columns: new[] { "Id", "IsAttivo", "IsCalcoloCarico", "Nome", "SquadraId", "ValoreMassimo", "ValoreMinimo" },
                values: new object[,]
                {
                    { 1, true, true, "RPE", null, 10, 0 },
                    { 2, true, false, "Sonno", null, 10, 0 },
                    { 3, true, false, "Dolore", null, 10, 0 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PreparatoriAtletici_SquadraId",
                table: "PreparatoriAtletici",
                column: "SquadraId");

            migrationBuilder.AddForeignKey(
                name: "FK_PreparatoriAtletici_Squadre_SquadraId",
                table: "PreparatoriAtletici",
                column: "SquadraId",
                principalTable: "Squadre",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PreparatoriAtletici_Squadre_SquadraId",
                table: "PreparatoriAtletici");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PreparatoriAtletici",
                table: "PreparatoriAtletici");

            migrationBuilder.DropIndex(
                name: "IX_PreparatoriAtletici_SquadraId",
                table: "PreparatoriAtletici");

            migrationBuilder.DeleteData(
                table: "Parametri",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Parametri",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Parametri",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DropColumn(
                name: "DurataTotaleMinuti",
                table: "Sessioni");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "Sessioni");

            migrationBuilder.DropColumn(
                name: "TempoEffettivoMinuti",
                table: "Sessioni");

            migrationBuilder.DropColumn(
                name: "IsAttivo",
                table: "Parametri");

            migrationBuilder.DropColumn(
                name: "IsCalcoloCarico",
                table: "Parametri");

            migrationBuilder.DropColumn(
                name: "SquadraId",
                table: "Parametri");

            migrationBuilder.DropColumn(
                name: "ValoreMassimo",
                table: "Parametri");

            migrationBuilder.DropColumn(
                name: "ValoreMinimo",
                table: "Parametri");

            migrationBuilder.DropColumn(
                name: "Altezza",
                table: "Atleti");

            migrationBuilder.DropColumn(
                name: "DescrizioneInfortunio",
                table: "Atleti");

            migrationBuilder.DropColumn(
                name: "FotoUrl",
                table: "Atleti");

            migrationBuilder.DropColumn(
                name: "IsAttivo",
                table: "Atleti");

            migrationBuilder.DropColumn(
                name: "IsInRiabilitazione",
                table: "Atleti");

            migrationBuilder.DropColumn(
                name: "IsInfortunato",
                table: "Atleti");

            migrationBuilder.DropColumn(
                name: "Password",
                table: "Atleti");

            migrationBuilder.DropColumn(
                name: "Peso",
                table: "Atleti");

            migrationBuilder.DropColumn(
                name: "ResetToken",
                table: "PreparatoriAtletici");

            migrationBuilder.DropColumn(
                name: "ResetTokenScadenza",
                table: "PreparatoriAtletici");

            migrationBuilder.DropColumn(
                name: "SquadraId",
                table: "PreparatoriAtletici");

            migrationBuilder.RenameTable(
                name: "PreparatoriAtletici",
                newName: "Preparatori");

            migrationBuilder.RenameColumn(
                name: "Password",
                table: "Preparatori",
                newName: "PasswordHash");

            migrationBuilder.AlterColumn<string>(
                name: "Valore",
                table: "ValoriSessione",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Data",
                table: "Sessioni",
                type: "text",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "TipologiaValore",
                table: "Parametri",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Preparatori",
                table: "Preparatori",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Sessioni_AtletaId",
                table: "Sessioni",
                column: "AtletaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sessioni_Atleti_AtletaId",
                table: "Sessioni",
                column: "AtletaId",
                principalTable: "Atleti",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
