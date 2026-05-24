using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Test_ONUS.Migrations
{
    /// <inheritdoc />
    public partial class PostgresSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Parametri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    ValoreMinimo = table.Column<int>(type: "integer", nullable: false),
                    ValoreMassimo = table.Column<int>(type: "integer", nullable: false),
                    IsAttivo = table.Column<bool>(type: "boolean", nullable: false),
                    IsCalcoloCarico = table.Column<bool>(type: "boolean", nullable: false),
                    SquadraId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parametri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sessioni",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AtletaId = table.Column<int>(type: "integer", nullable: false),
                    Data = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DurataTotaleMinuti = table.Column<int>(type: "integer", nullable: false),
                    TempoEffettivoMinuti = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessioni", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Squadre",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Squadre", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ValoriSessione",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessioneId = table.Column<int>(type: "integer", nullable: false),
                    ParametroId = table.Column<int>(type: "integer", nullable: false),
                    Valore = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValoriSessione", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValoriSessione_Parametri_ParametroId",
                        column: x => x.ParametroId,
                        principalTable: "Parametri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ValoriSessione_Sessioni_SessioneId",
                        column: x => x.SessioneId,
                        principalTable: "Sessioni",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Atleti",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Cognome = table.Column<string>(type: "text", nullable: false),
                    FotoUrl = table.Column<string>(type: "text", nullable: false),
                    IsAttivo = table.Column<bool>(type: "boolean", nullable: false),
                    Password = table.Column<string>(type: "text", nullable: false),
                    Peso = table.Column<double>(type: "double precision", nullable: false),
                    Altezza = table.Column<int>(type: "integer", nullable: false),
                    IsInfortunato = table.Column<bool>(type: "boolean", nullable: false),
                    IsInRiabilitazione = table.Column<bool>(type: "boolean", nullable: false),
                    DescrizioneInfortunio = table.Column<string>(type: "text", nullable: true),
                    SquadraId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Atleti", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Atleti_Squadre_SquadraId",
                        column: x => x.SquadraId,
                        principalTable: "Squadre",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PreparatoriAtletici",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Cognome = table.Column<string>(type: "text", nullable: false),
                    Password = table.Column<string>(type: "text", nullable: false),
                    SquadraId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreparatoriAtletici", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreparatoriAtletici_Squadre_SquadraId",
                        column: x => x.SquadraId,
                        principalTable: "Squadre",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "IX_Atleti_SquadraId",
                table: "Atleti",
                column: "SquadraId");

            migrationBuilder.CreateIndex(
                name: "IX_PreparatoriAtletici_SquadraId",
                table: "PreparatoriAtletici",
                column: "SquadraId");

            migrationBuilder.CreateIndex(
                name: "IX_ValoriSessione_ParametroId",
                table: "ValoriSessione",
                column: "ParametroId");

            migrationBuilder.CreateIndex(
                name: "IX_ValoriSessione_SessioneId",
                table: "ValoriSessione",
                column: "SessioneId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Atleti");

            migrationBuilder.DropTable(
                name: "PreparatoriAtletici");

            migrationBuilder.DropTable(
                name: "ValoriSessione");

            migrationBuilder.DropTable(
                name: "Squadre");

            migrationBuilder.DropTable(
                name: "Parametri");

            migrationBuilder.DropTable(
                name: "Sessioni");
        }
    }
}
