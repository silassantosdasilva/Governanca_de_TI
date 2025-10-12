using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Governança_de_TI.Migrations
{
    /// <inheritdoc />
    public partial class tabelaDescarte_add_ajustedescricao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Descricao",
                table: "Descartes");

            migrationBuilder.AddColumn<string>(
                name: "Observacao",
                table: "Descartes",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Observacao",
                table: "Descartes");

            migrationBuilder.AddColumn<string>(
                name: "Descricao",
                table: "Descartes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
