using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FliqPayroll.Data.Migrations
{
    /// <inheritdoc />
    public partial class RevampAttendanceRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HoursWorked",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "LateMinutes",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "OvertimeHours",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "RegularHolidayDays",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "SpecialNonWorkingDays",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "UndertimeMinutes",
                table: "AttendanceRecords");

            migrationBuilder.AddColumn<bool>(
                name: "IsLate",
                table: "AttendanceRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOvertimeValid",
                table: "AttendanceRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "OvertimeIn",
                table: "AttendanceRecords",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "OvertimeOut",
                table: "AttendanceRecords",
                type: "time",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLate",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "IsOvertimeValid",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "OvertimeIn",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "OvertimeOut",
                table: "AttendanceRecords");

            migrationBuilder.AddColumn<decimal>(
                name: "HoursWorked",
                table: "AttendanceRecords",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LateMinutes",
                table: "AttendanceRecords",
                type: "decimal(7,2)",
                precision: 7,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OvertimeHours",
                table: "AttendanceRecords",
                type: "decimal(7,2)",
                precision: 7,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RegularHolidayDays",
                table: "AttendanceRecords",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SpecialNonWorkingDays",
                table: "AttendanceRecords",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "AttendanceRecords",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "UndertimeMinutes",
                table: "AttendanceRecords",
                type: "decimal(7,2)",
                precision: 7,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
