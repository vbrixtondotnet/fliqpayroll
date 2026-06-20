# FliqPayroll

Payroll management system for **FLIQ Athletics**, built from the functional specification (`FS - FLIQ PAYROLL SYSTEM.pdf`).

## Solution structure

| Project | Responsibility |
|---|---|
| `FliqPayroll.Core` | Constants, DTOs, enums, repository interfaces, `PayrollCalculator` |
| `FliqPayroll.Data` | EF Core entities, `DbContext`, repositories, migrations, Identity user |
| `FliqPayroll.Services` | Business logic: employees, attendance, biometrics, payroll, reports |
| `FliqPayroll.Web` | ASP.NET MVC UI, REST API, jQuery/Ajax, payslip PDF (QuestPDF) |

## Modules (per functional spec)

### A. Employee Masterdata
- Full employee profile: personal, contact, employment, government contribution fields
- Search and filter (department, status, position, hire date, active/inactive)
- CRUD via modal UI with Ajax
- Export employees to CSV

### B. Biometrics Integration
- Upload CSV (`EmployeeCode, Date, TimeIn, TimeOut`)
- Match logs to employees, detect late/undertime/overtime
- Manual attendance review and adjustment before payroll

### C. Payroll Computation
- Bi-monthly cutoff (12th and 27th, configurable per period)
- Salary types: **Daily**, **Monthly**, **Fixed**
- Earnings: basic pay, overtime, holiday pay, leave with pay, incentives, allowances, bonuses, adjustments
- Deductions: absences, late, undertime, government contributions, loans, withholding tax
- Manual payroll adjustment, preview, period locking after approval

### D. Payroll Reports
- Payroll summary report
- Payslip generation (PDF)
- Employee payroll history
- Export summary and employees to CSV

### E. Security
- ASP.NET Identity with roles: Super Admin, HR Admin, Payroll Officer, Finance Officer
- Cookie-based login, audit trail logging
- Default admin: `admin@fliqpayroll.local` / `Admin@123`

## Pages

| Page | Route | Description |
|---|---|---|
| Dashboard | `/Dashboard` | KPI summary cards |
| Employees | `/Employees` | Masterdata CRUD + filters + export |
| Attendance | `/Attendance` | Daily sheet with manual edits |
| Biometrics | `/Biometrics` | Upload and validate biometric files |
| Payroll | `/Payroll` | Period payroll, recalculate, manual adjust, lock |
| Reports | `/Reports` | Summary, payslip PDF, history, CSV export |
| Login | `/Account/Login` | Secure login |

## Run locally

```bash
dotnet restore
dotnet build
dotnet ef database update --project src/FliqPayroll.Data
dotnet run --project src/FliqPayroll.Web
```

Migrations and seed data run automatically on startup.

## API endpoints

| Method | Route | Description |
|---|---|---|
| GET | `/api/dashboard/summary` | Dashboard metrics |
| GET | `/api/employees` | List/filter employees |
| GET | `/api/employees/departments` | Department list |
| POST/PUT/DELETE | `/api/employees` | Employee CRUD |
| GET | `/api/attendance?date=` | Daily attendance |
| GET | `/api/attendance/range?start=&end=` | Date range attendance |
| PUT | `/api/attendance/{id}` | Update attendance |
| POST | `/api/biometrics/upload` | Upload biometric CSV |
| GET | `/api/biometrics/summary` | Attendance summary |
| GET | `/api/payroll-periods` | List payroll periods |
| GET | `/api/payroll-periods/current` | Current period |
| POST | `/api/payroll-periods/ensure` | Ensure period for date |
| POST | `/api/payroll-periods/{id}/lock` | Lock period |
| GET | `/api/payroll?payrollPeriodId=` | Payroll for period |
| POST | `/api/payroll/recalculate` | Recalculate payroll |
| PUT | `/api/payroll/{id}` | Manual adjustment |
| GET | `/api/reports/summary/{periodId}` | Payroll summary |
| GET | `/api/reports/payslip/{payrollId}` | Payslip data |
| GET | `/api/reports/payslip/{payrollId}/pdf` | Payslip PDF |
| GET | `/api/reports/history/{employeeId}` | Employee payroll history |
| GET | `/api/reports/export/payroll/{periodId}` | Summary CSV |
| GET | `/api/reports/export/employees` | Employees CSV |

## Database migrations

```bash
dotnet ef migrations add <Name> --project src/FliqPayroll.Data
dotnet ef database update --project src/FliqPayroll.Data
```

## Architecture rules

- **Data** — EF models, DbContext, repository implementations only
- **Services** — business logic; no direct UI or HTTP concerns
- **Core** — shared DTOs, constants, interfaces, utilities
- **Web** — MVC views, Ajax scripts, API controllers, PDF rendering
