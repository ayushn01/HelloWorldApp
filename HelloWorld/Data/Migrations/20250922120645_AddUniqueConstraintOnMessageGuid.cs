using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelloWorld.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintOnMessageGuid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Messages_MessageGuid",
                table: "Messages",
                column: "MessageGuid",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Messages_MessageGuid",
                table: "Messages");
        }
    }
}
