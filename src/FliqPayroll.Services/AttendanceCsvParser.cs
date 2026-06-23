using System.Globalization;
using FliqPayroll.Core.Constants;
using FliqPayroll.Core.DTOs;
using FliqPayroll.Core.Utilities;

namespace FliqPayroll.Services;

internal static class AttendanceCsvParser
{
    private static readonly CultureInfo BiometricDateCulture = CultureInfo.GetCultureInfo("en-US");

    public static string NormalizeLine(string line)
    {
        if (string.IsNullOrEmpty(line))
        {
            return line;
        }

        return line[0] == '\uFEFF' ? line[1..] : line;
    }

    public static IReadOnlyList<string> ParseLine(string line)
    {
        var values = new List<string>();
        var current = string.Empty;
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                values.Add(current.Trim());
                current = string.Empty;
                continue;
            }

            current += ch;
        }

        values.Add(current.Trim());
        return values;
    }

    public static bool TryParsePunch(
        IReadOnlyList<string> columns,
        AttendanceCsvColumnMap columnMap,
        int lineNumber,
        ICollection<string> errors,
        out BiometricCsvPunchDto? punch)
    {
        punch = null;

        if (columns.Count <= columnMap.AttendanceCodeIndex)
        {
            errors.Add($"Line {lineNumber}: not enough columns.");
            return false;
        }

        if (IsHeaderRow(columns, columnMap))
        {
            return false;
        }

        var employeeCode = columns[columnMap.EmployeeCodeIndex].Trim();
        if (string.IsNullOrWhiteSpace(employeeCode))
        {
            errors.Add($"Line {lineNumber}: employee code is required.");
            return false;
        }

        if (!TryParseDate(columns[columnMap.DateIndex], out var date))
        {
            errors.Add($"Line {lineNumber}: invalid date '{columns[columnMap.DateIndex]}'.");
            return false;
        }

        if (!TryParseTime(columns[columnMap.TimeIndex], out var time))
        {
            errors.Add($"Line {lineNumber}: invalid time '{columns[columnMap.TimeIndex]}'.");
            return false;
        }

        if (!int.TryParse(columns[columnMap.AttendanceCodeIndex].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var attendanceCode))
        {
            errors.Add($"Line {lineNumber}: invalid attendance code '{columns[columnMap.AttendanceCodeIndex]}'.");
            return false;
        }

        if (attendanceCode is not (
            AttendanceConstants.CodeTimeIn or
            AttendanceConstants.CodeTimeOut or
            AttendanceConstants.CodeOvertimeIn or
            AttendanceConstants.CodeOvertimeOut))
        {
            return false;
        }

        punch = new BiometricCsvPunchDto
        {
            EmployeeCode = employeeCode,
            Date = date,
            Time = time,
            AttendanceCode = attendanceCode
        };

        return true;
    }

    private static bool IsHeaderRow(IReadOnlyList<string> columns, AttendanceCsvColumnMap columnMap)
    {
        if (columns.Count <= columnMap.AttendanceCodeIndex)
        {
            return false;
        }

        var employeeCode = columns[columnMap.EmployeeCodeIndex].Trim();
        var dateValue = columns[columnMap.DateIndex].Trim();
        var attendanceCode = columns[columnMap.AttendanceCodeIndex].Trim();

        if (employeeCode.Contains("employee", StringComparison.OrdinalIgnoreCase) ||
            dateValue.Contains("date", StringComparison.OrdinalIgnoreCase) ||
            attendanceCode.Contains("attendance", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return columns.Count > 0 &&
               columns[0].Equals("Column1", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseDate(string value, out DateTime date)
    {
        var trimmed = value.Trim();
        var formats = new[]
        {
            "MM/dd/yyyy", "M/d/yyyy", "MM/dd/yy", "M/d/yy",
            "dd/MM/yyyy", "d/M/yyyy",
            "yyyy-MM-dd", "yyyy/MM/dd", "yyyyMMdd", "MM-dd-yyyy", "M-d-yyyy"
        };

        if (DateTime.TryParseExact(trimmed, formats, BiometricDateCulture, DateTimeStyles.None, out date))
        {
            date = PhilippineTime.ForDateStorage(date);
            return true;
        }

        if (DateTime.TryParse(trimmed, BiometricDateCulture, DateTimeStyles.None, out date))
        {
            date = PhilippineTime.ForDateStorage(date);
            return true;
        }

        return false;
    }

    private static bool TryParseTime(string value, out TimeSpan time)
    {
        var trimmed = value.Trim();
        var formats = new[]
        {
            "h:mm:ss tt", "hh:mm:ss tt", "h:mm tt", "hh:mm tt",
            "HH:mm:ss", "H:mm:ss", "HH:mm", "H:mm"
        };

        if (DateTime.TryParseExact(trimmed, formats, BiometricDateCulture, DateTimeStyles.None, out var parsed))
        {
            time = parsed.TimeOfDay;
            return true;
        }

        if (TimeSpan.TryParse(trimmed, CultureInfo.InvariantCulture, out time))
        {
            return true;
        }

        return false;
    }
}
