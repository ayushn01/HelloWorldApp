using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelloWorld.Data.Migrations
{
    public partial class AddReadAtToMessage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReadAt",
                table: "Messages",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReadAt",
                table: "Messages");
        }
    }
}
