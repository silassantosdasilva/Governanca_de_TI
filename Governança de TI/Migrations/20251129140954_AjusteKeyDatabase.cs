using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Governança_de_TI.Migrations
{
    /// <inheritdoc />
    public partial class AjusteKeyDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdTipoLancamento",
                table: "TipoLancamento",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "IdSubcategoriaInt",
                table: "SubCategoria",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "IdPessoaInt",
                table: "Pessoas",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "IdParcelaFinanceiro",
                table: "LancamentosParcelas",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "IdLancamentoFinanceiro",
                table: "LancamentoFinanceiro",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "IdContaBancaria",
                table: "ContasBancarias",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdTipoLancamento",
                table: "TipoLancamento");

            migrationBuilder.DropColumn(
                name: "IdSubcategoriaInt",
                table: "SubCategoria");

            migrationBuilder.DropColumn(
                name: "IdPessoaInt",
                table: "Pessoas");

            migrationBuilder.DropColumn(
                name: "IdParcelaFinanceiro",
                table: "LancamentosParcelas");

            migrationBuilder.DropColumn(
                name: "IdLancamentoFinanceiro",
                table: "LancamentoFinanceiro");

            migrationBuilder.DropColumn(
                name: "IdContaBancaria",
                table: "ContasBancarias");
        }
    }
}
