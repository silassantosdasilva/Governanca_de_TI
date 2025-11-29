using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Governança_de_TI.Migrations
{
    /// <inheritdoc />
    public partial class AddSubCategoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LancamentosParcelas_LancamentosFinanceiros_IdLancamento",
                table: "LancamentosParcelas");

            migrationBuilder.DropTable(
                name: "LancamentosFinanceiros");

            migrationBuilder.DropTable(
                name: "TiposLancamento");

            migrationBuilder.CreateTable(
                name: "TipoLancamento",
                columns: table => new
                {
                    IdTipo = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoLancamento", x => x.IdTipo);
                });

            migrationBuilder.CreateTable(
                name: "SubCategoria",
                columns: table => new
                {
                    IdSubcategoria = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IdTipo = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubCategoria", x => x.IdSubcategoria);
                    table.ForeignKey(
                        name: "FK_SubCategoria_TipoLancamento_IdTipo",
                        column: x => x.IdTipo,
                        principalTable: "TipoLancamento",
                        principalColumn: "IdTipo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LancamentoFinanceiro",
                columns: table => new
                {
                    IdLancamento = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdPessoa = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdConta = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdTipo = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdSubcategoria = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TipoLancamento = table.Column<int>(type: "int", nullable: false),
                    ValorOriginal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Documento = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SequenciaDocumento = table.Column<int>(type: "int", nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataEmissao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataFluxo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FormaPagamento = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Condicao = table.Column<int>(type: "int", nullable: false),
                    NumeroParcelas = table.Column<int>(type: "int", nullable: false),
                    IntervaloDias = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LancamentoFinanceiro", x => x.IdLancamento);
                    table.ForeignKey(
                        name: "FK_LancamentoFinanceiro_ContasBancarias_IdConta",
                        column: x => x.IdConta,
                        principalTable: "ContasBancarias",
                        principalColumn: "IdConta",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LancamentoFinanceiro_Pessoas_IdPessoa",
                        column: x => x.IdPessoa,
                        principalTable: "Pessoas",
                        principalColumn: "IdPessoa",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LancamentoFinanceiro_SubCategoria_IdSubcategoria",
                        column: x => x.IdSubcategoria,
                        principalTable: "SubCategoria",
                        principalColumn: "IdSubcategoria");
                    table.ForeignKey(
                        name: "FK_LancamentoFinanceiro_TipoLancamento_IdTipo",
                        column: x => x.IdTipo,
                        principalTable: "TipoLancamento",
                        principalColumn: "IdTipo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LancamentoFinanceiro_IdConta",
                table: "LancamentoFinanceiro",
                column: "IdConta");

            migrationBuilder.CreateIndex(
                name: "IX_LancamentoFinanceiro_IdPessoa",
                table: "LancamentoFinanceiro",
                column: "IdPessoa");

            migrationBuilder.CreateIndex(
                name: "IX_LancamentoFinanceiro_IdSubcategoria",
                table: "LancamentoFinanceiro",
                column: "IdSubcategoria");

            migrationBuilder.CreateIndex(
                name: "IX_LancamentoFinanceiro_IdTipo",
                table: "LancamentoFinanceiro",
                column: "IdTipo");

            migrationBuilder.CreateIndex(
                name: "IX_SubCategoria_IdTipo",
                table: "SubCategoria",
                column: "IdTipo");

            migrationBuilder.AddForeignKey(
                name: "FK_LancamentosParcelas_LancamentoFinanceiro_IdLancamento",
                table: "LancamentosParcelas",
                column: "IdLancamento",
                principalTable: "LancamentoFinanceiro",
                principalColumn: "IdLancamento",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LancamentosParcelas_LancamentoFinanceiro_IdLancamento",
                table: "LancamentosParcelas");

            migrationBuilder.DropTable(
                name: "LancamentoFinanceiro");

            migrationBuilder.DropTable(
                name: "SubCategoria");

            migrationBuilder.DropTable(
                name: "TipoLancamento");

            migrationBuilder.CreateTable(
                name: "TiposLancamento",
                columns: table => new
                {
                    IdTipo = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposLancamento", x => x.IdTipo);
                });

            migrationBuilder.CreateTable(
                name: "LancamentosFinanceiros",
                columns: table => new
                {
                    IdLancamento = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContaIdConta = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PessoaIdPessoa = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoIdTipo = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Condicao = table.Column<int>(type: "int", nullable: false),
                    DataEmissao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataFluxo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Documento = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FormaPagamento = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdConta = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdPessoa = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdTipo = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntervaloDias = table.Column<int>(type: "int", nullable: false),
                    NumeroParcelas = table.Column<int>(type: "int", nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SequenciaDocumento = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TipoLancamento = table.Column<int>(type: "int", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorOriginal = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LancamentosFinanceiros", x => x.IdLancamento);
                    table.ForeignKey(
                        name: "FK_LancamentosFinanceiros_ContasBancarias_ContaIdConta",
                        column: x => x.ContaIdConta,
                        principalTable: "ContasBancarias",
                        principalColumn: "IdConta",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LancamentosFinanceiros_Pessoas_PessoaIdPessoa",
                        column: x => x.PessoaIdPessoa,
                        principalTable: "Pessoas",
                        principalColumn: "IdPessoa",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LancamentosFinanceiros_TiposLancamento_TipoIdTipo",
                        column: x => x.TipoIdTipo,
                        principalTable: "TiposLancamento",
                        principalColumn: "IdTipo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LancamentosFinanceiros_ContaIdConta",
                table: "LancamentosFinanceiros",
                column: "ContaIdConta");

            migrationBuilder.CreateIndex(
                name: "IX_LancamentosFinanceiros_PessoaIdPessoa",
                table: "LancamentosFinanceiros",
                column: "PessoaIdPessoa");

            migrationBuilder.CreateIndex(
                name: "IX_LancamentosFinanceiros_TipoIdTipo",
                table: "LancamentosFinanceiros",
                column: "TipoIdTipo");

            migrationBuilder.AddForeignKey(
                name: "FK_LancamentosParcelas_LancamentosFinanceiros_IdLancamento",
                table: "LancamentosParcelas",
                column: "IdLancamento",
                principalTable: "LancamentosFinanceiros",
                principalColumn: "IdLancamento",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
