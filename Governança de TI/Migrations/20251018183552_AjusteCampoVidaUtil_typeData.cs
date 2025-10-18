using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Governança_de_TI.Migrations
{
    /// <inheritdoc />
    public partial class AjusteCampoVidaUtil_typeData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VidaUtilAnos",
                table: "Equipamentos");

            migrationBuilder.AddColumn<DateTime>(
                name: "VidaUtil",
                table: "Equipamentos",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VidaUtil",
                table: "Equipamentos");

            migrationBuilder.AddColumn<int>(
                name: "VidaUtilAnos",
                table: "Equipamentos",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
