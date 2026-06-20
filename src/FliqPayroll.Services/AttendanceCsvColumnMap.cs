using FliqPayroll.Core.Constants;

namespace FliqPayroll.Services;

internal sealed class AttendanceCsvColumnMap
{
    public int EmployeeCodeIndex { get; }
    public int DateIndex { get; }
    public int TimeIndex { get; }
    public int AttendanceCodeIndex { get; }

    public AttendanceCsvColumnMap(int employeeCodeIndex, int dateIndex, int timeIndex, int attendanceCodeIndex)
    {
        EmployeeCodeIndex = employeeCodeIndex;
        DateIndex = dateIndex;
        TimeIndex = timeIndex;
        AttendanceCodeIndex = attendanceCodeIndex;
    }

    public static AttendanceCsvColumnMap Default { get; } = new(
        AttendanceConstants.CsvColumnEmployeeCode,
        AttendanceConstants.CsvColumnDate,
        AttendanceConstants.CsvColumnTime,
        AttendanceConstants.CsvColumnAttendanceCode);

    public static bool TryParseHeader(IReadOnlyList<string> columns, out AttendanceCsvColumnMap map)
    {
        map = Default;
        int? employeeCodeIndex = null;
        int? dateIndex = null;
        int? timeIndex = null;
        int? attendanceCodeIndex = null;

        for (var i = 0; i < columns.Count; i++)
        {
            var header = columns[i].Trim();
            if (string.IsNullOrWhiteSpace(header))
            {
                continue;
            }

            if (header.Contains("employee", StringComparison.OrdinalIgnoreCase) &&
                header.Contains("code", StringComparison.OrdinalIgnoreCase))
            {
                employeeCodeIndex = i;
                continue;
            }

            if (header.Equals("date", StringComparison.OrdinalIgnoreCase) ||
                header.Contains("date", StringComparison.OrdinalIgnoreCase))
            {
                dateIndex = i;
                continue;
            }

            if (header.Equals("time", StringComparison.OrdinalIgnoreCase))
            {
                timeIndex = i;
                continue;
            }

            if (header.Contains("attendance", StringComparison.OrdinalIgnoreCase) &&
                header.Contains("code", StringComparison.OrdinalIgnoreCase))
            {
                attendanceCodeIndex = i;
            }
        }

        if (employeeCodeIndex is null || dateIndex is null || timeIndex is null || attendanceCodeIndex is null)
        {
            return false;
        }

        map = new AttendanceCsvColumnMap(
            employeeCodeIndex.Value,
            dateIndex.Value,
            timeIndex.Value,
            attendanceCodeIndex.Value);

        return true;
    }
}
