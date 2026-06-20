using FliqPayroll.Core.DTOs;
using FliqPayroll.Core.Interfaces;
using FliqPayroll.Core.Utilities;
using FliqPayroll.Services.Interfaces;

namespace FliqPayroll.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IEmployeeRepository _employeeRepository;

    public AttendanceService(
        IAttendanceRepository attendanceRepository,
        IEmployeeRepository employeeRepository)
    {
        _attendanceRepository = Guard.AgainstNull(attendanceRepository, nameof(attendanceRepository));
        _employeeRepository = Guard.AgainstNull(employeeRepository, nameof(employeeRepository));
    }

    public Task<IReadOnlyList<AttendanceDto>> GetSheetAsync(DateTime date, CancellationToken cancellationToken = default) =>
        _attendanceRepository.GetByDateAsync(PhilippineTime.ToPhilippineDate(date), cancellationToken);

    public Task<IReadOnlyList<AttendanceDto>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var phStart = PhilippineTime.ToPhilippineDate(startDate);
        var phEnd = PhilippineTime.ToPhilippineDate(endDate);

        if (phEnd < phStart)
        {
            throw new ArgumentException("End date must be on or after start date.", nameof(endDate));
        }

        return _attendanceRepository.GetByDateRangeAsync(phStart, phEnd, cancellationToken);
    }

    public Task<AttendanceDto?> UpdateAsync(UpdateAttendanceDto dto, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(dto, nameof(dto));
        return _attendanceRepository.UpdateAsync(dto, cancellationToken);
    }

    public async Task<AttendanceUploadResultDto> UploadCsvAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNull(fileStream, nameof(fileStream));
        var safeFileName = Guard.AgainstNullOrWhiteSpace(fileName, nameof(fileName));

        var errors = new List<string>();
        var punches = new List<BiometricCsvPunchDto>();
        var totalRows = 0;
        var unmatchedRows = 0;

        var employeesByCode = await _employeeRepository.GetEmployeeIdsByCodesAsync(cancellationToken);

        using var reader = new StreamReader(fileStream);
        var lineNumber = 0;
        var columnMap = AttendanceCsvColumnMap.Default;
        var headerResolved = false;

        while (await reader.ReadLineAsync(cancellationToken) is { } rawLine)
        {
            lineNumber++;
            var line = AttendanceCsvParser.NormalizeLine(rawLine);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var columns = AttendanceCsvParser.ParseLine(line);

            if (!headerResolved)
            {
                if (AttendanceCsvColumnMap.TryParseHeader(columns, out var headerMap))
                {
                    columnMap = headerMap;
                    headerResolved = true;
                    continue;
                }

                headerResolved = true;
            }

            totalRows++;

            if (!AttendanceCsvParser.TryParsePunch(columns, columnMap, lineNumber, errors, out var punch) || punch is null)
            {
                continue;
            }

            if (!employeesByCode.TryGetValue(punch.EmployeeCode, out _))
            {
                unmatchedRows++;
                errors.Add($"Line {lineNumber}: no employee with EmployeeCode '{punch.EmployeeCode}' was found.");
                continue;
            }

            punches.Add(punch);
        }

        var grouped = punches
            .GroupBy(p => new { p.EmployeeCode, Date = PhilippineTime.ToPhilippineDate(p.Date) })
            .ToList();

        var processedDays = 0;
        var skippedIncomplete = 0;

        foreach (var group in grouped)
        {
            if (!employeesByCode.TryGetValue(group.Key.EmployeeCode, out var employeeId))
            {
                continue;
            }

            var processed = AttendancePolicyProcessor.ProcessDayPunches(
                employeeId,
                group.Key.EmployeeCode,
                group.Key.Date,
                group.ToList());

            if (processed is null)
            {
                skippedIncomplete++;
                continue;
            }

            await _attendanceRepository.UpsertBiometricAsync(processed, cancellationToken);
            processedDays++;
        }

        var uploadId = await _attendanceRepository.SaveUploadLogAsync(
            safeFileName,
            totalRows,
            processedDays,
            skippedIncomplete,
            unmatchedRows,
            cancellationToken);

        return new AttendanceUploadResultDto
        {
            UploadId = uploadId,
            FileName = safeFileName,
            TotalRows = totalRows,
            ProcessedDays = processedDays,
            SkippedIncomplete = skippedIncomplete,
            UnmatchedRows = unmatchedRows,
            Errors = errors
        };
    }

    public async Task<AttendanceSummaryDto> GetSummaryAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var phStart = PhilippineTime.ToPhilippineDate(startDate);
        var phEnd = PhilippineTime.ToPhilippineDate(endDate);

        if (phEnd < phStart)
        {
            throw new ArgumentException("End date must be on or after start date.", nameof(endDate));
        }

        var records = await _attendanceRepository.GetByDateRangeAsync(phStart, phEnd, cancellationToken);

        return new AttendanceSummaryDto
        {
            StartDate = phStart,
            EndDate = phEnd,
            TotalEmployees = records.Select(r => r.EmployeeId).Distinct().Count(),
            ValidAttendanceDays = records.Count(r => r.IsAttendanceValid),
            LateDays = records.Count(r => r.IsLate),
            IncompleteDays = records.Count(r => !r.IsAttendanceValid),
            TotalOvertimeHours = records.Sum(r => r.OvertimeHours)
        };
    }
}
