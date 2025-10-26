using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Governança_de_TI.Migrations
{
    /// <inheritdoc />
    public partial class CriarTablesGamificacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Gamificacoes",
                columns: table => new
                {
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Pontos = table.Column<int>(type: "int", nullable: false),
                    Nivel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gamificacoes", x => x.UsuarioId);
                    table.ForeignKey(
                        name: "FK_Gamificacoes_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Premios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PontosNecessarios = table.Column<int>(type: "int", nullable: false),
                    IconeBootstrap = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Premios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioPremios",
                columns: table => new
                {
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    PremioId = table.Column<int>(type: "int", nullable: false),
                    DataConquista = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioPremios", x => new { x.UsuarioId, x.PremioId });
                    table.ForeignKey(
                        name: "FK_UsuarioPremios_Premios_PremioId",
                        column: x => x.PremioId,
                        principalTable: "Premios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuarioPremios_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioPremios_PremioId",
                table: "UsuarioPremios",
                column: "PremioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Gamificacoes");

            migrationBuilder.DropTable(
                name: "UsuarioPremios");

            migrationBuilder.DropTable(
                name: "Premios");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios");
        }
    }
}
