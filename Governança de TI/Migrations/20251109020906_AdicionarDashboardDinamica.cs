using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Governança_de_TI.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarDashboardDinamica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DashboardWidgets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titulo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Posicao = table.Column<int>(type: "int", nullable: false),
                    TipoVisualizacao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TabelaFonte = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CampoMetrica = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CampoDimensao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Operacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CamposLista = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrdenarPor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Ordem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Campo1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OperadorAvancado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Campo2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UsuarioId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardWidgets", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DashboardWidgets");
        }
    }
}
