using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FliqPayroll.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPasswordChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PasswordChange",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordChange",
                table: "AspNetUsers");
        }
    }
}
