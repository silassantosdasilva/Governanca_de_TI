using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Governança_de_TI.Migrations
{
    /// <inheritdoc />
    public partial class AjusteFinalCriacaoUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Gamificacoes_Usuarios_UsuarioId",
                table: "Gamificacoes");

            migrationBuilder.AddForeignKey(
                name: "FK_Gamificacoes_Usuarios_UsuarioId",
                table: "Gamificacoes",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Gamificacoes_Usuarios_UsuarioId",
                table: "Gamificacoes");

            migrationBuilder.AddForeignKey(
                name: "FK_Gamificacoes_Usuarios_UsuarioId",
                table: "Gamificacoes",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
