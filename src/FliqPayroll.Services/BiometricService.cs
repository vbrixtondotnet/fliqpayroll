using FliqPayroll.Core.DTOs;
using FliqPayroll.Core.Utilities;
using FliqPayroll.Services.Interfaces;

namespace FliqPayroll.Services;

public class BiometricService : IBiometricService
{
    private readonly IAttendanceService _attendanceService;

    public BiometricService(IAttendanceService attendanceService)
    {
        _attendanceService = Guard.AgainstNull(attendanceService, nameof(attendanceService));
    }

    public Task<AttendanceUploadResultDto> UploadAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default) =>
        _attendanceService.UploadCsvAsync(fileStream, fileName, cancellationToken);

    public Task<AttendanceSummaryDto> GetAttendanceSummaryAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default) =>
        _attendanceService.GetSummaryAsync(startDate, endDate, cancellationToken);
}
