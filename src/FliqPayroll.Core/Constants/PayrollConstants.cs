namespace FliqPayroll.Core.Constants;

public static class PayrollConstants
{
    public const int DefaultFirstCutoffDay = 12;
    public const int DefaultSecondCutoffDay = 27;
    public const int WorkingDaysPerCutoff = 13;
    public const int MonthlyWorkingDaysDivisor = 26;
    public const int HoursPerDay = 8;

    public const decimal RegularOvertimeRate = 1.25m;
    public const decimal RegularHolidayPresentMultiplier = 2.00m;
    public const decimal SpecialNonWorkingRate = 1.30m;
    public const decimal RegularHolidayRate = 1.00m;

    public const decimal DefaultTaxRate = 0.10m;
}
