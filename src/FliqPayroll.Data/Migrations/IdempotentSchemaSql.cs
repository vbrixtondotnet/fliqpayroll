namespace FliqPayroll.Data.Migrations;

internal static class IdempotentSchemaSql
{
    public const string Up = """
        -- ASP.NET Identity (skip if already created by a prior migration attempt)
        IF OBJECT_ID(N'[AspNetRoles]', N'U') IS NULL
        BEGIN
            CREATE TABLE [AspNetRoles] (
                [Id] nvarchar(450) NOT NULL,
                [Name] nvarchar(256) NULL,
                [NormalizedName] nvarchar(256) NULL,
                [ConcurrencyStamp] nvarchar(max) NULL,
                CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
            );
            CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles]([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
        END;

        IF OBJECT_ID(N'[AspNetUsers]', N'U') IS NULL
        BEGIN
            CREATE TABLE [AspNetUsers] (
                [Id] nvarchar(450) NOT NULL,
                [FullName] nvarchar(max) NULL,
                [IsActive] bit NOT NULL,
                [CreatedAt] datetime2 NOT NULL,
                [UserName] nvarchar(256) NULL,
                [NormalizedUserName] nvarchar(256) NULL,
                [Email] nvarchar(256) NULL,
                [NormalizedEmail] nvarchar(256) NULL,
                [EmailConfirmed] bit NOT NULL,
                [PasswordHash] nvarchar(max) NULL,
                [SecurityStamp] nvarchar(max) NULL,
                [ConcurrencyStamp] nvarchar(max) NULL,
                [PhoneNumber] nvarchar(max) NULL,
                [PhoneNumberConfirmed] bit NOT NULL,
                [TwoFactorEnabled] bit NOT NULL,
                [LockoutEnd] datetimeoffset NULL,
                [LockoutEnabled] bit NOT NULL,
                [AccessFailedCount] int NOT NULL,
                CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
            );
            CREATE INDEX [EmailIndex] ON [AspNetUsers]([NormalizedEmail]);
            CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers]([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
        END;

        IF OBJECT_ID(N'[AspNetRoleClaims]', N'U') IS NULL
        BEGIN
            CREATE TABLE [AspNetRoleClaims] (
                [Id] int NOT NULL IDENTITY,
                [RoleId] nvarchar(450) NOT NULL,
                [ClaimType] nvarchar(max) NULL,
                [ClaimValue] nvarchar(max) NULL,
                CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles]([Id]) ON DELETE CASCADE
            );
            CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims]([RoleId]);
        END;

        IF OBJECT_ID(N'[AspNetUserClaims]', N'U') IS NULL
        BEGIN
            CREATE TABLE [AspNetUserClaims] (
                [Id] int NOT NULL IDENTITY,
                [UserId] nvarchar(450) NOT NULL,
                [ClaimType] nvarchar(max) NULL,
                [ClaimValue] nvarchar(max) NULL,
                CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
            );
            CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims]([UserId]);
        END;

        IF OBJECT_ID(N'[AspNetUserLogins]', N'U') IS NULL
        BEGIN
            CREATE TABLE [AspNetUserLogins] (
                [LoginProvider] nvarchar(450) NOT NULL,
                [ProviderKey] nvarchar(450) NOT NULL,
                [ProviderDisplayName] nvarchar(max) NULL,
                [UserId] nvarchar(450) NOT NULL,
                CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
                CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
            );
            CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins]([UserId]);
        END;

        IF OBJECT_ID(N'[AspNetUserRoles]', N'U') IS NULL
        BEGIN
            CREATE TABLE [AspNetUserRoles] (
                [UserId] nvarchar(450) NOT NULL,
                [RoleId] nvarchar(450) NOT NULL,
                CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
                CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles]([Id]) ON DELETE CASCADE,
                CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
            );
            CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles]([RoleId]);
        END;

        IF OBJECT_ID(N'[AspNetUserTokens]', N'U') IS NULL
        BEGIN
            CREATE TABLE [AspNetUserTokens] (
                [UserId] nvarchar(450) NOT NULL,
                [LoginProvider] nvarchar(450) NOT NULL,
                [Name] nvarchar(450) NOT NULL,
                [Value] nvarchar(max) NULL,
                CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
                CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE
            );
        END;

        IF OBJECT_ID(N'[AuditLogs]', N'U') IS NULL
        BEGIN
            CREATE TABLE [AuditLogs] (
                [Id] int NOT NULL IDENTITY,
                [UserName] nvarchar(100) NOT NULL,
                [Action] nvarchar(100) NOT NULL,
                [EntityName] nvarchar(100) NOT NULL,
                [EntityId] nvarchar(50) NULL,
                [Details] nvarchar(2000) NULL,
                [CreatedAt] datetime2 NOT NULL,
                CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
            );
        END;

        IF OBJECT_ID(N'[BiometricUploads]', N'U') IS NULL
        BEGIN
            CREATE TABLE [BiometricUploads] (
                [Id] int NOT NULL IDENTITY,
                [FileName] nvarchar(255) NOT NULL,
                [StartDate] datetime2 NOT NULL,
                [EndDate] datetime2 NOT NULL,
                [TotalRows] int NOT NULL,
                [MatchedRows] int NOT NULL,
                [UnmatchedRows] int NOT NULL,
                [UploadedBy] nvarchar(max) NULL,
                [UploadedAt] datetime2 NOT NULL,
                CONSTRAINT [PK_BiometricUploads] PRIMARY KEY ([Id])
            );
        END;

        IF OBJECT_ID(N'[Employees]', N'U') IS NULL
        BEGIN
            CREATE TABLE [Employees] (
                [Id] int NOT NULL IDENTITY,
                [EmployeeCode] nvarchar(20) NOT NULL,
                [LastName] nvarchar(100) NOT NULL,
                [FirstName] nvarchar(100) NOT NULL,
                [MiddleName] nvarchar(100) NULL,
                [Gender] int NULL,
                [CivilStatus] int NULL,
                [DateOfBirth] datetime2 NULL,
                [Nationality] nvarchar(max) NULL,
                [Religion] nvarchar(max) NULL,
                [MobileNumber] nvarchar(30) NULL,
                [Email] nvarchar(200) NULL,
                [HomeAddress] nvarchar(500) NULL,
                [EmergencyContactPerson] nvarchar(max) NULL,
                [EmergencyContactNumber] nvarchar(max) NULL,
                [EmergencyContactRelationship] nvarchar(max) NULL,
                [EmploymentStatus] int NOT NULL,
                [Department] nvarchar(100) NULL,
                [Position] nvarchar(100) NULL,
                [Supervisor] nvarchar(100) NULL,
                [HireDate] datetime2 NOT NULL,
                [DateRegularized] datetime2 NULL,
                [SalaryType] int NOT NULL,
                [BasicSalary] decimal(18,2) NOT NULL,
                [PayrollFrequency] int NOT NULL,
                [BankName] nvarchar(100) NULL,
                [BankAccountNumber] nvarchar(50) NULL,
                [TinNumber] nvarchar(30) NULL,
                [SssNumber] nvarchar(30) NULL,
                [SssErShare] decimal(18,2) NOT NULL,
                [SssEeShare] decimal(18,2) NOT NULL,
                [SssLoan] decimal(18,2) NOT NULL,
                [PhilHealthNumber] nvarchar(30) NULL,
                [PhilHealthErShare] decimal(18,2) NOT NULL,
                [PhilHealthEeShare] decimal(18,2) NOT NULL,
                [PagIbigNumber] nvarchar(30) NULL,
                [PagIbigErShare] decimal(18,2) NOT NULL,
                [PagIbigEeShare] decimal(18,2) NOT NULL,
                [PagIbigLoan] decimal(18,2) NOT NULL,
                [IsActive] bit NOT NULL,
                [CreatedAt] datetime2 NOT NULL,
                [UpdatedAt] datetime2 NULL,
                CONSTRAINT [PK_Employees] PRIMARY KEY ([Id])
            );
            CREATE UNIQUE INDEX [IX_Employees_EmployeeCode] ON [Employees]([EmployeeCode]);
        END
        ELSE
        BEGIN
            IF COL_LENGTH('Employees', 'MonthlySalary') IS NOT NULL AND COL_LENGTH('Employees', 'BasicSalary') IS NULL
                EXEC sp_rename 'Employees.MonthlySalary', 'BasicSalary', 'COLUMN';

            IF COL_LENGTH('Employees', 'MiddleName') IS NULL ALTER TABLE [Employees] ADD [MiddleName] nvarchar(100) NULL;
            IF COL_LENGTH('Employees', 'Gender') IS NULL ALTER TABLE [Employees] ADD [Gender] int NULL;
            IF COL_LENGTH('Employees', 'CivilStatus') IS NULL ALTER TABLE [Employees] ADD [CivilStatus] int NULL;
            IF COL_LENGTH('Employees', 'DateOfBirth') IS NULL ALTER TABLE [Employees] ADD [DateOfBirth] datetime2 NULL;
            IF COL_LENGTH('Employees', 'Nationality') IS NULL ALTER TABLE [Employees] ADD [Nationality] nvarchar(max) NULL;
            IF COL_LENGTH('Employees', 'Religion') IS NULL ALTER TABLE [Employees] ADD [Religion] nvarchar(max) NULL;
            IF COL_LENGTH('Employees', 'MobileNumber') IS NULL ALTER TABLE [Employees] ADD [MobileNumber] nvarchar(30) NULL;
            IF COL_LENGTH('Employees', 'HomeAddress') IS NULL ALTER TABLE [Employees] ADD [HomeAddress] nvarchar(500) NULL;
            IF COL_LENGTH('Employees', 'EmergencyContactPerson') IS NULL ALTER TABLE [Employees] ADD [EmergencyContactPerson] nvarchar(max) NULL;
            IF COL_LENGTH('Employees', 'EmergencyContactNumber') IS NULL ALTER TABLE [Employees] ADD [EmergencyContactNumber] nvarchar(max) NULL;
            IF COL_LENGTH('Employees', 'EmergencyContactRelationship') IS NULL ALTER TABLE [Employees] ADD [EmergencyContactRelationship] nvarchar(max) NULL;
            IF COL_LENGTH('Employees', 'EmploymentStatus') IS NULL ALTER TABLE [Employees] ADD [EmploymentStatus] int NOT NULL CONSTRAINT [DF_Employees_EmploymentStatus] DEFAULT 0;
            IF COL_LENGTH('Employees', 'Supervisor') IS NULL ALTER TABLE [Employees] ADD [Supervisor] nvarchar(100) NULL;
            IF COL_LENGTH('Employees', 'DateRegularized') IS NULL ALTER TABLE [Employees] ADD [DateRegularized] datetime2 NULL;
            IF COL_LENGTH('Employees', 'SalaryType') IS NULL ALTER TABLE [Employees] ADD [SalaryType] int NOT NULL CONSTRAINT [DF_Employees_SalaryType] DEFAULT 0;
            IF COL_LENGTH('Employees', 'BasicSalary') IS NULL ALTER TABLE [Employees] ADD [BasicSalary] decimal(18,2) NOT NULL CONSTRAINT [DF_Employees_BasicSalary] DEFAULT 0;
            IF COL_LENGTH('Employees', 'PayrollFrequency') IS NULL ALTER TABLE [Employees] ADD [PayrollFrequency] int NOT NULL CONSTRAINT [DF_Employees_PayrollFrequency] DEFAULT 0;
            IF COL_LENGTH('Employees', 'BankName') IS NULL ALTER TABLE [Employees] ADD [BankName] nvarchar(100) NULL;
            IF COL_LENGTH('Employees', 'BankAccountNumber') IS NULL ALTER TABLE [Employees] ADD [BankAccountNumber] nvarchar(50) NULL;
            IF COL_LENGTH('Employees', 'TinNumber') IS NULL ALTER TABLE [Employees] ADD [TinNumber] nvarchar(30) NULL;
            IF COL_LENGTH('Employees', 'SssNumber') IS NULL ALTER TABLE [Employees] ADD [SssNumber] nvarchar(30) NULL;
            IF COL_LENGTH('Employees', 'SssErShare') IS NULL ALTER TABLE [Employees] ADD [SssErShare] decimal(18,2) NOT NULL CONSTRAINT [DF_Employees_SssErShare] DEFAULT 0;
            IF COL_LENGTH('Employees', 'SssEeShare') IS NULL ALTER TABLE [Employees] ADD [SssEeShare] decimal(18,2) NOT NULL CONSTRAINT [DF_Employees_SssEeShare] DEFAULT 0;
            IF COL_LENGTH('Employees', 'SssLoan') IS NULL ALTER TABLE [Employees] ADD [SssLoan] decimal(18,2) NOT NULL CONSTRAINT [DF_Employees_SssLoan] DEFAULT 0;
            IF COL_LENGTH('Employees', 'PhilHealthNumber') IS NULL ALTER TABLE [Employees] ADD [PhilHealthNumber] nvarchar(30) NULL;
            IF COL_LENGTH('Employees', 'PhilHealthErShare') IS NULL ALTER TABLE [Employees] ADD [PhilHealthErShare] decimal(18,2) NOT NULL CONSTRAINT [DF_Employees_PhilHealthErShare] DEFAULT 0;
            IF COL_LENGTH('Employees', 'PhilHealthEeShare') IS NULL ALTER TABLE [Employees] ADD [PhilHealthEeShare] decimal(18,2) NOT NULL CONSTRAINT [DF_Employees_PhilHealthEeShare] DEFAULT 0;
            IF COL_LENGTH('Employees', 'PagIbigNumber') IS NULL ALTER TABLE [Employees] ADD [PagIbigNumber] nvarchar(30) NULL;
            IF COL_LENGTH('Employees', 'PagIbigErShare') IS NULL ALTER TABLE [Employees] ADD [PagIbigErShare] decimal(18,2) NOT NULL CONSTRAINT [DF_Employees_PagIbigErShare] DEFAULT 0;
            IF COL_LENGTH('Employees', 'PagIbigEeShare') IS NULL ALTER TABLE [Employees] ADD [PagIbigEeShare] decimal(18,2) NOT NULL CONSTRAINT [DF_Employees_PagIbigEeShare] DEFAULT 0;
            IF COL_LENGTH('Employees', 'PagIbigLoan') IS NULL ALTER TABLE [Employees] ADD [PagIbigLoan] decimal(18,2) NOT NULL CONSTRAINT [DF_Employees_PagIbigLoan] DEFAULT 0;
        END;

        IF OBJECT_ID(N'[PayrollRecords]', N'U') IS NOT NULL DROP TABLE [PayrollRecords];
        IF OBJECT_ID(N'[PayrollPeriods]', N'U') IS NOT NULL DROP TABLE [PayrollPeriods];

        IF OBJECT_ID(N'[AttendanceRecords]', N'U') IS NULL
        BEGIN
            CREATE TABLE [AttendanceRecords] (
                [Id] int NOT NULL IDENTITY,
                [EmployeeId] int NOT NULL,
                [Date] datetime2 NOT NULL,
                [TimeIn] time NULL,
                [TimeOut] time NULL,
                [IsLate] bit NOT NULL,
                [OvertimeIn] time NULL,
                [OvertimeOut] time NULL,
                [IsOvertimeValid] bit NOT NULL,
                [IsFromBiometric] bit NOT NULL,
                [Notes] nvarchar(500) NULL,
                [CreatedAt] datetime2 NOT NULL,
                [UpdatedAt] datetime2 NULL,
                CONSTRAINT [PK_AttendanceRecords] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_AttendanceRecords_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees]([Id]) ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX [IX_AttendanceRecords_EmployeeId_Date] ON [AttendanceRecords]([EmployeeId], [Date]);
        END
        ELSE
        BEGIN
            IF COL_LENGTH('AttendanceRecords', 'IsLate') IS NULL ALTER TABLE [AttendanceRecords] ADD [IsLate] bit NOT NULL CONSTRAINT [DF_AttendanceRecords_IsLate] DEFAULT 0;
            IF COL_LENGTH('AttendanceRecords', 'OvertimeIn') IS NULL ALTER TABLE [AttendanceRecords] ADD [OvertimeIn] time NULL;
            IF COL_LENGTH('AttendanceRecords', 'OvertimeOut') IS NULL ALTER TABLE [AttendanceRecords] ADD [OvertimeOut] time NULL;
            IF COL_LENGTH('AttendanceRecords', 'IsOvertimeValid') IS NULL ALTER TABLE [AttendanceRecords] ADD [IsOvertimeValid] bit NOT NULL CONSTRAINT [DF_AttendanceRecords_IsOvertimeValid] DEFAULT 0;
            IF COL_LENGTH('AttendanceRecords', 'TimeIn') IS NULL ALTER TABLE [AttendanceRecords] ADD [TimeIn] time NULL;
            IF COL_LENGTH('AttendanceRecords', 'TimeOut') IS NULL ALTER TABLE [AttendanceRecords] ADD [TimeOut] time NULL;
            IF COL_LENGTH('AttendanceRecords', 'IsFromBiometric') IS NULL ALTER TABLE [AttendanceRecords] ADD [IsFromBiometric] bit NOT NULL CONSTRAINT [DF_AttendanceRecords_IsFromBiometric] DEFAULT 0;

            IF COL_LENGTH('AttendanceRecords', 'Status') IS NOT NULL ALTER TABLE [AttendanceRecords] DROP COLUMN [Status];
            IF COL_LENGTH('AttendanceRecords', 'HoursWorked') IS NOT NULL ALTER TABLE [AttendanceRecords] DROP COLUMN [HoursWorked];
            IF COL_LENGTH('AttendanceRecords', 'LateMinutes') IS NOT NULL ALTER TABLE [AttendanceRecords] DROP COLUMN [LateMinutes];
            IF COL_LENGTH('AttendanceRecords', 'UndertimeMinutes') IS NOT NULL ALTER TABLE [AttendanceRecords] DROP COLUMN [UndertimeMinutes];
            IF COL_LENGTH('AttendanceRecords', 'OvertimeHours') IS NOT NULL ALTER TABLE [AttendanceRecords] DROP COLUMN [OvertimeHours];
            IF COL_LENGTH('AttendanceRecords', 'RegularHolidayDays') IS NOT NULL ALTER TABLE [AttendanceRecords] DROP COLUMN [RegularHolidayDays];
            IF COL_LENGTH('AttendanceRecords', 'SpecialNonWorkingDays') IS NOT NULL ALTER TABLE [AttendanceRecords] DROP COLUMN [SpecialNonWorkingDays];
        END;

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
        END;

        -- Clear stale migration history from removed migrations so EF state stays consistent
        DELETE FROM [__EFMigrationsHistory]
        WHERE [MigrationId] IN (N'20260608055437_InitialCreate', N'20260608120000_FullFunctionalSpec');
        """;

    public const string Down = """
        IF OBJECT_ID(N'[PayrollRecords]', N'U') IS NOT NULL DROP TABLE [PayrollRecords];
        IF OBJECT_ID(N'[AttendanceRecords]', N'U') IS NOT NULL DROP TABLE [AttendanceRecords];
        IF OBJECT_ID(N'[PayrollPeriods]', N'U') IS NOT NULL DROP TABLE [PayrollPeriods];
        IF OBJECT_ID(N'[BiometricUploads]', N'U') IS NOT NULL DROP TABLE [BiometricUploads];
        IF OBJECT_ID(N'[AuditLogs]', N'U') IS NOT NULL DROP TABLE [AuditLogs];
        IF OBJECT_ID(N'[Employees]', N'U') IS NOT NULL DROP TABLE [Employees];
        IF OBJECT_ID(N'[AspNetUserTokens]', N'U') IS NOT NULL DROP TABLE [AspNetUserTokens];
        IF OBJECT_ID(N'[AspNetUserRoles]', N'U') IS NOT NULL DROP TABLE [AspNetUserRoles];
        IF OBJECT_ID(N'[AspNetUserLogins]', N'U') IS NOT NULL DROP TABLE [AspNetUserLogins];
        IF OBJECT_ID(N'[AspNetUserClaims]', N'U') IS NOT NULL DROP TABLE [AspNetUserClaims];
        IF OBJECT_ID(N'[AspNetRoleClaims]', N'U') IS NOT NULL DROP TABLE [AspNetRoleClaims];
        IF OBJECT_ID(N'[AspNetUsers]', N'U') IS NOT NULL DROP TABLE [AspNetUsers];
        IF OBJECT_ID(N'[AspNetRoles]', N'U') IS NOT NULL DROP TABLE [AspNetRoles];
        """;
}
