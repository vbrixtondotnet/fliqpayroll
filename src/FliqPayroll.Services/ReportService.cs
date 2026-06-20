using System.Globalization;

using System.Text;

using FliqPayroll.Core.DTOs;

using FliqPayroll.Core.Interfaces;

using FliqPayroll.Core.Utilities;

using FliqPayroll.Services.Interfaces;



namespace FliqPayroll.Services;



public class ReportService : IReportService

{

    private readonly IPayrollService _payrollService;

    private readonly IEmployeeRepository _employeeRepository;



    public ReportService(

        IPayrollService payrollService,

        IEmployeeRepository employeeRepository)

    {

        _payrollService = Guard.AgainstNull(payrollService, nameof(payrollService));

        _employeeRepository = Guard.AgainstNull(employeeRepository, nameof(employeeRepository));

    }



    public async Task<PayrollSummaryReportDto> GetPayrollSummaryAsync(

        DateTime fromDate,

        DateTime toDate,

        CancellationToken cancellationToken = default)

    {

        var payroll = await _payrollService.GetByDateRangeAsync(fromDate, toDate, cancellationToken);

        var period = PayrollPeriodHelper.CreateRange(fromDate, toDate);



        return new PayrollSummaryReportDto

        {

            Period = period,

            Records = payroll.Records,

            TotalGrossPay = payroll.Records.Sum(r => r.GrossPay),

            TotalDeductions = payroll.Records.Sum(r => r.TotalDeductions),

            TotalNetPay = payroll.Records.Sum(r => r.NetPay)

        };

    }



    public async Task<PayslipDto?> GetPayslipAsync(

        int employeeId,

        DateTime fromDate,

        DateTime toDate,

        CancellationToken cancellationToken = default)

    {

        var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken);

        if (employee is null)

        {

            return null;

        }



        var payroll = await _payrollService.GetByDateRangeAsync(fromDate, toDate, cancellationToken);

        var record = payroll.Records.FirstOrDefault(r => r.EmployeeId == employeeId);

        if (record is null)

        {

            return null;

        }



        return new PayslipDto

        {

            Payroll = record,

            Period = PayrollPeriodHelper.CreateRange(fromDate, toDate),

            Employee = employee

        };

    }



    public async Task<EmployeePayrollHistoryDto?> GetEmployeeHistoryAsync(

        int employeeId,

        CancellationToken cancellationToken = default)

    {

        var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken);

        if (employee is null)

        {

            return null;

        }



        return new EmployeePayrollHistoryDto

        {

            Employee = employee,

            History = []

        };

    }



    public async Task<byte[]> ExportPayrollSummaryCsvAsync(

        DateTime fromDate,

        DateTime toDate,

        CancellationToken cancellationToken = default)

    {

        var summary = await GetPayrollSummaryAsync(fromDate, toDate, cancellationToken);

        var builder = new StringBuilder();



        builder.AppendLine(string.Join(",",

            "Employee Code",

            "Employee Name",

            "Position",

            "Working Days",

            "Absent Days",

            "Gross Pay",

            "Total Deductions",

            "Net Pay",

            "Status"));



        foreach (var record in summary.Records)

        {

            builder.AppendLine(string.Join(",",

                CsvEscape(record.EmployeeCode),

                CsvEscape(record.EmployeeName),

                CsvEscape(record.Position),

                record.WorkingDays.ToString(CultureInfo.InvariantCulture),

                record.AbsentDays.ToString(CultureInfo.InvariantCulture),

                record.GrossPay.ToString("F2", CultureInfo.InvariantCulture),

                record.TotalDeductions.ToString("F2", CultureInfo.InvariantCulture),

                record.NetPay.ToString("F2", CultureInfo.InvariantCulture),

                CsvEscape(record.Status.ToString())));

        }



        return Encoding.UTF8.GetBytes(builder.ToString());

    }



    public async Task<byte[]> ExportEmployeesCsvAsync(

        EmployeeFilterDto? filter = null,

        CancellationToken cancellationToken = default)

    {

        var employees = await _employeeRepository.GetAllAsync(filter, cancellationToken);

        var builder = new StringBuilder();



        builder.AppendLine(string.Join(",",

            "Employee Code",

            "Last Name",

            "First Name",

            "Department",

            "Position",

            "Employment Status",

            "Salary Type",

            "Basic Salary",

            "Hire Date",

            "Active"));



        foreach (var employee in employees)

        {

            builder.AppendLine(string.Join(",",

                CsvEscape(employee.EmployeeCode),

                CsvEscape(employee.LastName),

                CsvEscape(employee.FirstName),

                CsvEscape(employee.Department),

                CsvEscape(employee.Position),

                CsvEscape(employee.EmploymentStatus.ToString()),

                CsvEscape(employee.SalaryType.ToString()),

                employee.BasicSalary.ToString("F2", CultureInfo.InvariantCulture),

                employee.HireDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),

                employee.IsActive ? "Yes" : "No"));

        }



        return Encoding.UTF8.GetBytes(builder.ToString());

    }



    private static string CsvEscape(string? value)

    {

        if (string.IsNullOrEmpty(value))

        {

            return string.Empty;

        }



        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))

        {

            return $"\"{value.Replace("\"", "\"\"")}\"";

        }



        return value;

    }

}


