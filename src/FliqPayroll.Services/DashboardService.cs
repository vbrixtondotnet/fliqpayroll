using FliqPayroll.Core.DTOs;

using FliqPayroll.Core.Interfaces;

using FliqPayroll.Core.Utilities;

using FliqPayroll.Services.Interfaces;



namespace FliqPayroll.Services;



public class DashboardService : IDashboardService

{

    private readonly IEmployeeRepository _employeeRepository;

    private readonly IAttendanceRepository _attendanceRepository;

    private readonly IPayrollService _payrollService;



    public DashboardService(

        IEmployeeRepository employeeRepository,

        IAttendanceRepository attendanceRepository,

        IPayrollService payrollService)

    {

        _employeeRepository = Guard.AgainstNull(employeeRepository, nameof(employeeRepository));

        _attendanceRepository = Guard.AgainstNull(attendanceRepository, nameof(attendanceRepository));

        _payrollService = Guard.AgainstNull(payrollService, nameof(payrollService));

    }



    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)

    {

        var today = PhilippineTime.Today;

        var currentPeriod = PayrollPeriodHelper.ResolveDefaultPeriod(today);



        var totalEmployees = await _employeeRepository.CountAllAsync(cancellationToken);

        var activeEmployees = await _employeeRepository.CountActiveAsync(cancellationToken);

        var presentToday = await _attendanceRepository.CountValidByDateAsync(today, cancellationToken);

        var absentToday = Math.Max(0, activeEmployees - presentToday);



        var payroll = await _payrollService.GetByDateRangeAsync(

            currentPeriod.StartDate,

            currentPeriod.EndDate,

            cancellationToken);



        return new DashboardSummaryDto

        {

            TotalEmployees = totalEmployees,

            ActiveEmployees = activeEmployees,

            PresentToday = presentToday,

            AbsentToday = absentToday,

            TotalMonthlyPayroll = payroll.Records.Sum(r => r.NetPay),

            PendingPayrollCount = 0

        };

    }

}


