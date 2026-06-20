using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FliqPayroll.Data.Migrations;

/// <summary>
/// Idempotent schema migration safe for fresh databases and legacy databases
/// that already contain Identity or InitialCreate tables.
/// </summary>
public partial class Initial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(IdempotentSchemaSql.Up);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(IdempotentSchemaSql.Down);
    }
}
