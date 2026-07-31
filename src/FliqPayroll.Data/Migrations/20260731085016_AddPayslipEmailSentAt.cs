using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FliqPayroll.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPayslipEmailSentAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PayslipEmailSentAt",
                table: "PayrollRecords",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PayslipEmailSentAt",
                table: "PayrollRecords");
        }
    }
}
