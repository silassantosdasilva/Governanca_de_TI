using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Governança_de_TI.Migrations
{
    /// <inheritdoc />
    public partial class AjusteDashboardDinamica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CampoDataFiltro",
                table: "DashboardWidgets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataFiltroFim",
                table: "DashboardWidgets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataFiltroInicio",
                table: "DashboardWidgets",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CampoDataFiltro",
                table: "DashboardWidgets");

            migrationBuilder.DropColumn(
                name: "DataFiltroFim",
                table: "DashboardWidgets");

            migrationBuilder.DropColumn(
                name: "DataFiltroInicio",
                table: "DashboardWidgets");
        }
    }
}
