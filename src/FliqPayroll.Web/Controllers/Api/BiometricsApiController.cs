using FliqPayroll.Core.DTOs;
using FliqPayroll.Core.Utilities;
using FliqPayroll.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FliqPayroll.Web.Controllers.Api;

[ApiController]
[Route("api/biometrics")]
[AllowAnonymous]
public class BiometricsApiController : ControllerBase
{
    private readonly IBiometricService _biometricService;

    public BiometricsApiController(IBiometricService biometricService)
    {
        _biometricService = biometricService;
    }

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
            var result = await _biometricService.UploadAsync(stream, file.FileName, cancellationToken);
            return Ok(ApiResult<AttendanceUploadResultDto>.Ok(result, "Biometric file processed."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResult<AttendanceUploadResultDto>.Fail(ex.Message));
        }
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResult<AttendanceSummaryDto>>> GetSummary(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        try
        {
            var summary = await _biometricService.GetAttendanceSummaryAsync(
                PhilippineTime.ToPhilippineDate(startDate),
                PhilippineTime.ToPhilippineDate(endDate),
                cancellationToken);
            return Ok(ApiResult<AttendanceSummaryDto>.Ok(summary));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResult<AttendanceSummaryDto>.Fail(ex.Message));
        }
    }
}
