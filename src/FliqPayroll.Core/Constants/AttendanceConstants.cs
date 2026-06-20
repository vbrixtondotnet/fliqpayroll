namespace FliqPayroll.Core.Constants;

public static class AttendanceConstants
{
    public const int CodeTimeIn = 0;
    public const int CodeTimeOut = 1;
    public const int CodeOvertimeIn = 4;
    public const int CodeOvertimeOut = 5;

    public static readonly TimeSpan WorkStart = new(8, 0, 0);
    public static readonly TimeSpan WorkEnd = new(17, 0, 0);
    /// <summary>Grace period ends at 8:15 AM Philippine Time (UTC+8).</summary>
    public static readonly TimeSpan GracePeriodEnd = new(8, 15, 0);
    /// <summary>Late threshold starts at 8:16 AM Philippine Time (UTC+8).</summary>
    public static readonly TimeSpan LateThreshold = new(8, 16, 0);

    // Biometric export: Column1 (unused), Employee Code, Date, Time, Column5 (ignored), Attendance Code, ...
    public const int CsvColumnEmployeeCode = 1;
    public const int CsvColumnDate = 2;
    public const int CsvColumnTime = 3;
    public const int CsvColumnAttendanceCode = 5;
}
