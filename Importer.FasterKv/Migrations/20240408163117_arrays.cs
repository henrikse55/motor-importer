using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Importer.FasterKv.Migrations
{
    /// <inheritdoc />
    public partial class arrays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ObjectName",
                table: "MotorEntries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Dictionary<string, string>>(
                name: "RelatedArrays",
                table: "MotorEntries",
                type: "hstore",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ObjectName",
                table: "MotorEntries");

            migrationBuilder.DropColumn(
                name: "RelatedArrays",
                table: "MotorEntries");
        }
    }
}
