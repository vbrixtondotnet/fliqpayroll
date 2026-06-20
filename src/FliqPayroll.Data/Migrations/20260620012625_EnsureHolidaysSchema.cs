using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FliqPayroll.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnsureHolidaysSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[PayrollRecords]', N'U') IS NOT NULL
                    DROP TABLE [PayrollRecords];

                IF OBJECT_ID(N'[PayrollPeriods]', N'U') IS NOT NULL
                    DROP TABLE [PayrollPeriods];

                IF OBJECT_ID(N'[Holidays]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [Holidays] (
                        [HolidayId] int NOT NULL IDENTITY,
                        [Date] datetime2 NOT NULL,
                        [Description] nvarchar(200) NOT NULL,
                        [HolidayType] int NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [UpdatedAt] datetime2 NULL,
                        CONSTRAINT [PK_Holidays] PRIMARY KEY ([HolidayId])
                    );
                    CREATE UNIQUE INDEX [IX_Holidays_Date] ON [Holidays]([Date]);
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[Holidays]', N'U') IS NOT NULL
                    DROP TABLE [Holidays];
                """);
        }
    }
}
