using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Governança_de_TI.Migrations
{
    /// <inheritdoc />
    public partial class tabelaDescarte_add : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Descartes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EquipamentoId = table.Column<int>(type: "int", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantidade = table.Column<int>(type: "int", nullable: false),
                    DataColeta = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EmpresaColetora = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CnpjEmpresa = table.Column<string>(type: "nvarchar(18)", maxLength: 18, nullable: true),
                    EmailEmpresa = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PessoaResponsavelColeta = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CertificadoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EnviarEmail = table.Column<bool>(type: "bit", nullable: false),
                    DataDeCadastro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Descartes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Descartes_Equipamentos_EquipamentoId",
                        column: x => x.EquipamentoId,
                        principalTable: "Equipamentos",
                        principalColumn: "CodigoItem",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Descartes_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Descartes_EquipamentoId",
                table: "Descartes",
                column: "EquipamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Descartes_UsuarioId",
                table: "Descartes",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Descartes");
        }
    }
}
