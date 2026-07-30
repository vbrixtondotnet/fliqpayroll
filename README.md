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

## Gmail setup (Email Payslip)

Payslip emails are sent from `fliqdeveloper@gmail.com` through MailKit over Gmail SMTP. Two
authentication modes are supported. If `Gmail:AppPassword` is set, SMTP authenticates with it and
the OAuth flow is bypassed entirely; otherwise OAuth 2.0 is used. Check the active mode at
`/admin/gmail/status`.

Secrets must never be committed to `appsettings.json`, which holds placeholders only.

### Option A — App Password (no Google verification required)

Gmail SMTP over OAuth needs the `https://mail.google.com/` restricted scope, which Google gates
behind a paid security assessment. An App Password avoids that entirely.

1. Enable 2-Step Verification on `fliqdeveloper@gmail.com`.
2. Create an App Password at <https://myaccount.google.com/apppasswords>.
3. Store it via User Secrets (spaces are stripped automatically):

```bash
dotnet user-secrets set "Gmail:AppPassword" "<16-char-app-password>" --project src/FliqPayroll.Web
```

### Option B — OAuth 2.0

1. In Google Cloud Console, create an OAuth **Web application** client and add the authorized redirect URI:
   - Local: `https://localhost:7073/admin/gmail/callback`
   - Production: `https://<your-host>/admin/gmail/callback`
2. On the OAuth consent screen, add `https://mail.google.com/` to the scopes and add the sender
   account as a test user.
3. Store credentials via User Secrets:

```bash
dotnet user-secrets set "Gmail:ClientId" "<client-id>" --project src/FliqPayroll.Web
dotnet user-secrets set "Gmail:ClientSecret" "<client-secret>" --project src/FliqPayroll.Web
```

4. Sign in as SuperAdmin or HRAdmin and open `/admin/gmail/connect` once to grant offline access.
   Tokens are encrypted with ASP.NET Data Protection and stored under `App_Data/` (gitignored).

While the consent screen's publishing status is **Testing**, Google expires refresh tokens after
7 days and the sender must be reconnected. Publish the app to production to avoid this.

### Sending

On the Payslips page, use **Email Payslip** beside each PDF button. The button is disabled with
tooltip **Missing Email Address** when the employee has no email on file.

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
| POST | `/api/reports/payslip/email?employeeId=&payrollPeriodId=` | Email Employee Copy PDF |
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
