using FliqPayroll.Core.DTOs;
using FliqPayroll.Core.Utilities;
using FliqPayroll.Services.Interfaces;
using FliqPayroll.Web.Jobs;
using Hangfire;

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;



namespace FliqPayroll.Web.Controllers.Api;



[ApiController]

[Route("api/payroll")]


public class PayrollApiController : ControllerBase

{

    private readonly IPayrollService _payrollService;
    private readonly IBackgroundJobClient _backgroundJobs;
    private readonly IConfiguration _configuration;

    public PayrollApiController(
        IPayrollService payrollService,
        IBackgroundJobClient backgroundJobs,
        IConfiguration configuration)
    {
        _payrollService = payrollService;
        _backgroundJobs = backgroundJobs;
        _configuration = configuration;
    }



    [HttpGet("defaultPeriod")]

    public async Task<ActionResult<ApiResult<PayrollPeriodDto>>> GetDefaultPeriod(

        [FromQuery] DateTime? referenceDate,

        CancellationToken cancellationToken)

    {

        var period = await _payrollService.GetDefaultPeriodAsync(
            referenceDate.HasValue ? PhilippineTime.ToPhilippineDate(referenceDate.Value) : null,
            cancellationToken);

        return Ok(ApiResult<PayrollPeriodDto>.Ok(period));

    }



    [HttpGet("getByDateRange")]

    public async Task<ActionResult<ApiResult<PayrollByDateRangeDto>>> GetByDateRange(

        [FromQuery] string? fromDate,

        [FromQuery] string? toDate,

        CancellationToken cancellationToken)

    {

        if (!PhilippineTime.TryParseCalendarDate(fromDate, out var parsedFromDate) ||

            !PhilippineTime.TryParseCalendarDate(toDate, out var parsedToDate))

        {

            return BadRequest(ApiResult<PayrollByDateRangeDto>.Fail("Invalid date format. Use YYYY-MM-DD."));

        }

        try

        {

            var result = await _payrollService.GetByDateRangeAsync(
                AttendanceDateHelper.ToCalendarDate(parsedFromDate),
                AttendanceDateHelper.ToCalendarDate(parsedToDate),
                cancellationToken);

            return Ok(ApiResult<PayrollByDateRangeDto>.Ok(result));

        }

        catch (ArgumentException ex)

        {

            return BadRequest(ApiResult<PayrollByDateRangeDto>.Fail(ex.Message));

        }

        catch (InvalidOperationException ex)

        {

            return BadRequest(ApiResult<PayrollByDateRangeDto>.Fail(ex.Message));

        }

    }

    [HttpGet("savedPeriods")]
    public async Task<ActionResult<ApiResult<IReadOnlyList<SavePayrollPeriodResultDto>>>> GetSavedPeriods(
        CancellationToken cancellationToken)
    {
        var periods = await _payrollService.GetSavedPeriodsAsync(cancellationToken);
        return Ok(ApiResult<IReadOnlyList<SavePayrollPeriodResultDto>>.Ok(periods));
    }

    [HttpPost("savePeriod")]
    public async Task<ActionResult<ApiResult<SavePayrollPeriodResultDto>>> SavePeriod(
        [FromBody] SavePayrollPeriodRequestDto? request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(ApiResult<SavePayrollPeriodResultDto>.Fail("Request body is required."));
        }

        try
        {
            var result = await _payrollService.SavePeriodAsync(
                new SavePayrollPeriodRequestDto
                {
                    FromDate = PhilippineTime.ToPhilippineDate(request.FromDate),
                    ToDate = PhilippineTime.ToPhilippineDate(request.ToDate),
                    PeriodName = request.PeriodName,
                    Records = request.Records,
                    RequestedBy = User.Identity?.Name ?? "Anonymous"
                },
                cancellationToken);

            if (result.NotifyApprovers)
            {
                // The first pending change of a period schedules ONE consolidated email. The short delay lets a burst
                // of edits (auto-save) be reported together; the report is built from the database when the job runs.
                var delaySeconds = Math.Max(0, _configuration.GetValue("Approvals:EmailDelaySeconds", 120));
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                _backgroundJobs.Schedule<PayrollApprovalEmailJobs>(
                    job => job.SendAsync(result.PayrollPeriodId, baseUrl, null),
                    TimeSpan.FromSeconds(delaySeconds));
            }

            var message = result.SubmittedForApprovalCount > 0
                ? $"{result.SubmittedForApprovalCount} edited row(s) were submitted for approval. " +
                  "The changes take effect once a Finance Officer approves them."
                : result.PendingApprovalCount > 0
                    ? "No new changes. Some rows are still waiting for Finance Officer approval."
                    : "Payroll period saved successfully.";

            return Ok(ApiResult<SavePayrollPeriodResultDto>.Ok(result, message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResult<SavePayrollPeriodResultDto>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResult<SavePayrollPeriodResultDto>.Fail(ex.Message));
        }
    }

    [HttpGet("export/excel")]
    public Task<IActionResult> ExportExcel(
        [FromQuery] string? from,
        [FromQuery] string? to,
        CancellationToken cancellationToken) =>
        ExportAsync(from, to, "excel", cancellationToken);

    [HttpGet("export/csv")]
    public Task<IActionResult> ExportCsv(
        [FromQuery] string? from,
        [FromQuery] string? to,
        CancellationToken cancellationToken) =>
        ExportAsync(from, to, "csv", cancellationToken);

    [HttpGet("export/pdf")]
    public Task<IActionResult> ExportPdf(
        [FromQuery] string? from,
        [FromQuery] string? to,
        CancellationToken cancellationToken) =>
        ExportAsync(from, to, "pdf", cancellationToken);

    private async Task<IActionResult> ExportAsync(
        string? from,
        string? to,
        string format,
        CancellationToken cancellationToken)
    {
        if (!PhilippineTime.TryParseCalendarDate(from, out var parsedFrom) ||
            !PhilippineTime.TryParseCalendarDate(to, out var parsedTo))
        {
            return BadRequest("Invalid date format. Use YYYY-MM-DD.");
        }

        try
        {
            var fromDate = AttendanceDateHelper.ToCalendarDate(parsedFrom);
            var toDate = AttendanceDateHelper.ToCalendarDate(parsedTo);
            var periodLabel = FormatExportPeriod(fromDate, toDate);

            byte[] bytes;
            string contentType;
            string extension;

            switch (format)
            {
                case "excel":
                    bytes = await _payrollService.ExportExcelAsync(fromDate, toDate, cancellationToken);
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    extension = "xlsx";
                    break;
                case "csv":
                    bytes = await _payrollService.ExportCsvAsync(fromDate, toDate, cancellationToken);
                    contentType = "text/csv";
                    extension = "csv";
                    break;
                case "pdf":
                    bytes = await _payrollService.ExportPdfAsync(fromDate, toDate, cancellationToken);
                    contentType = "application/pdf";
                    extension = "pdf";
                    break;
                default:
                    return BadRequest("Unsupported export format.");
            }

            var fileName = $"Payroll - {periodLabel}.{extension}";
            return File(bytes, contentType, fileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private static string FormatExportPeriod(DateTime fromDate, DateTime toDate)
    {
        var start = PhilippineTime.ToPhilippineDate(fromDate);
        var end = PhilippineTime.ToPhilippineDate(toDate);

        if (start.Year == end.Year && start.Month == end.Month)
        {
            return $"{start:MMMM} {start.Day}-{end.Day}, {end.Year}";
        }

        if (start.Year == end.Year)
        {
            return $"{start:MMMM d} - {end:MMMM d, yyyy}";
        }

        return $"{start:MMMM d, yyyy} - {end:MMMM d, yyyy}";
    }
}


