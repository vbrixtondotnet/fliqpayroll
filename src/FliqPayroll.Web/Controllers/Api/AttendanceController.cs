using FliqPayroll.Core.DTOs;
using FliqPayroll.Core.Utilities;
using FliqPayroll.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FliqPayroll.Web.Controllers.Api;

[ApiController]
[Route("api/attendance")]
[AllowAnonymous]
public class AttendanceApiController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceApiController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResult<IReadOnlyList<AttendanceDto>>>> GetByDateRange(
        [FromQuery] string? fromDate,
        [FromQuery] string? toDate,
        CancellationToken cancellationToken)
    {
        if (!PhilippineTime.TryParseCalendarDate(fromDate, out var parsedFromDate) ||
            !PhilippineTime.TryParseCalendarDate(toDate, out var parsedToDate))
        {
            return BadRequest(ApiResult<IReadOnlyList<AttendanceDto>>.Fail("Invalid date format. Use YYYY-MM-DD for From and To."));
        }

        var startDate = AttendanceDateHelper.ToCalendarDate(parsedFromDate);
        var endDate = AttendanceDateHelper.ToCalendarDate(parsedToDate);

        if (endDate < startDate)
        {
            return BadRequest(ApiResult<IReadOnlyList<AttendanceDto>>.Fail("To date must be on or after From date."));
        }

        try
        {
            var records = await _attendanceService.GetByDateRangeAsync(startDate, endDate, cancellationToken);
            return Ok(ApiResult<IReadOnlyList<AttendanceDto>>.Ok(records));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResult<IReadOnlyList<AttendanceDto>>.Fail(ex.Message));
        }
    }

    [HttpGet("range")]
    public Task<ActionResult<ApiResult<IReadOnlyList<AttendanceDto>>>> GetByDateRangeLegacy(
        [FromQuery] string? fromDate,
        [FromQuery] string? toDate,
        CancellationToken cancellationToken) =>
        GetByDateRange(fromDate, toDate, cancellationToken);

    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResult<AttendanceUploadResultDto>>> Upload(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResult<AttendanceUploadResultDto>.Fail("File is required."));
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _attendanceService.UploadCsvAsync(stream, file.FileName, cancellationToken);
            return Ok(ApiResult<AttendanceUploadResultDto>.Ok(result, "Attendance file processed."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResult<AttendanceUploadResultDto>.Fail(ex.Message));
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResult<AttendanceDto>>> Update(
        int id,
        [FromBody] UpdateAttendanceDto dto,
        CancellationToken cancellationToken)
    {
        if (dto is null)
        {
            return BadRequest(ApiResult<AttendanceDto>.Fail("Request body is required."));
        }

        dto.Id = id;
        var updated = await _attendanceService.UpdateAsync(dto, cancellationToken);

        if (updated is null)
        {
            return NotFound(ApiResult<AttendanceDto>.Fail("Attendance record not found."));
        }

        return Ok(ApiResult<AttendanceDto>.Ok(updated, "Attendance updated."));
    }
}
