using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Governança_de_TI.Migrations
{
    /// <inheritdoc />
    public partial class AjustePessoaModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Observacao",
                table: "LancamentosFinanceiros",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FormaPagamento",
                table: "LancamentosFinanceiros",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ContaIdConta",
                table: "LancamentosFinanceiros",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "PessoaIdPessoa",
                table: "LancamentosFinanceiros",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TipoIdTipo",
                table: "LancamentosFinanceiros",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

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
                name: "FK_LancamentosFinanceiros_ContasBancarias_ContaIdConta",
                table: "LancamentosFinanceiros",
                column: "ContaIdConta",
                principalTable: "ContasBancarias",
                principalColumn: "IdConta",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LancamentosFinanceiros_Pessoas_PessoaIdPessoa",
                table: "LancamentosFinanceiros",
                column: "PessoaIdPessoa",
                principalTable: "Pessoas",
                principalColumn: "IdPessoa",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LancamentosFinanceiros_TiposLancamento_TipoIdTipo",
                table: "LancamentosFinanceiros",
                column: "TipoIdTipo",
                principalTable: "TiposLancamento",
                principalColumn: "IdTipo",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LancamentosFinanceiros_ContasBancarias_ContaIdConta",
                table: "LancamentosFinanceiros");

            migrationBuilder.DropForeignKey(
                name: "FK_LancamentosFinanceiros_Pessoas_PessoaIdPessoa",
                table: "LancamentosFinanceiros");

            migrationBuilder.DropForeignKey(
                name: "FK_LancamentosFinanceiros_TiposLancamento_TipoIdTipo",
                table: "LancamentosFinanceiros");

            migrationBuilder.DropIndex(
                name: "IX_LancamentosFinanceiros_ContaIdConta",
                table: "LancamentosFinanceiros");

            migrationBuilder.DropIndex(
                name: "IX_LancamentosFinanceiros_PessoaIdPessoa",
                table: "LancamentosFinanceiros");

            migrationBuilder.DropIndex(
                name: "IX_LancamentosFinanceiros_TipoIdTipo",
                table: "LancamentosFinanceiros");

            migrationBuilder.DropColumn(
                name: "ContaIdConta",
                table: "LancamentosFinanceiros");

            migrationBuilder.DropColumn(
                name: "PessoaIdPessoa",
                table: "LancamentosFinanceiros");

            migrationBuilder.DropColumn(
                name: "TipoIdTipo",
                table: "LancamentosFinanceiros");

            migrationBuilder.AlterColumn<string>(
                name: "Observacao",
                table: "LancamentosFinanceiros",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "FormaPagamento",
                table: "LancamentosFinanceiros",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
