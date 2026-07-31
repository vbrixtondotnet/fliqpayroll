using FliqPayroll.Core.DTOs;
using FliqPayroll.Core.Utilities;
using FliqPayroll.Services.Interfaces;
using FliqPayroll.Web.Jobs;
using FliqPayroll.Web.Services;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FliqPayroll.Web.Controllers.Api;

[ApiController]
[Route("api/reports")]
[AllowAnonymous]
public class ReportsApiController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly PayslipPdfService _payslipPdfService;
    private readonly IBackgroundJobClient _backgroundJobs;

    public ReportsApiController(
        IReportService reportService,
        PayslipPdfService payslipPdfService,
        IBackgroundJobClient backgroundJobs)
    {
        _reportService = reportService;
        _payslipPdfService = payslipPdfService;
        _backgroundJobs = backgroundJobs;
    }



    [HttpGet("payroll-summary")]

    public async Task<ActionResult<ApiResult<PayrollSummaryReportDto>>> GetPayrollSummary(

        [FromQuery] int? payrollPeriodId,

        [FromQuery] DateTime? fromDate,

        [FromQuery] DateTime? toDate,

        CancellationToken cancellationToken)

    {

        try

        {

            if (payrollPeriodId.HasValue)

            {

                var savedReport = await _reportService.GetPayrollSummaryByPeriodIdAsync(

                    payrollPeriodId.Value,

                    cancellationToken);

                return Ok(ApiResult<PayrollSummaryReportDto>.Ok(savedReport));

            }



            if (!fromDate.HasValue || !toDate.HasValue)

            {

                return BadRequest(ApiResult<PayrollSummaryReportDto>.Fail("Select a saved payroll period."));

            }



            var phFrom = PhilippineTime.ToPhilippineDate(fromDate.Value);
            var phTo = PhilippineTime.ToPhilippineDate(toDate.Value);

            var report = await _reportService.GetPayrollSummaryAsync(phFrom, phTo, cancellationToken);

            return Ok(ApiResult<PayrollSummaryReportDto>.Ok(report));

        }

        catch (ArgumentException ex)

        {

            return BadRequest(ApiResult<PayrollSummaryReportDto>.Fail(ex.Message));

        }

    }



    [HttpGet("payslip")]

    public async Task<ActionResult<ApiResult<PayslipDto>>> GetPayslip(

        [FromQuery] int employeeId,

        [FromQuery] DateTime fromDate,

        [FromQuery] DateTime toDate,

        CancellationToken cancellationToken)

    {

        var phFrom = PhilippineTime.ToPhilippineDate(fromDate);
        var phTo = PhilippineTime.ToPhilippineDate(toDate);

        var payslip = await _reportService.GetPayslipAsync(employeeId, phFrom, phTo, cancellationToken);

        if (payslip is null)

        {

            return NotFound(ApiResult<PayslipDto>.Fail("Payslip not found."));

        }



        return Ok(ApiResult<PayslipDto>.Ok(payslip));

    }



    [HttpGet("payslip/pdf")]

    public async Task<IActionResult> GetPayslipPdf(

        [FromQuery] int employeeId,

        [FromQuery] int? payrollPeriodId,

        [FromQuery] DateTime? fromDate,

        [FromQuery] DateTime? toDate,

        CancellationToken cancellationToken)

    {

        PayslipDto? payslip;

        if (payrollPeriodId.HasValue)

        {

            payslip = await _reportService.GetPayslipByPeriodIdAsync(

                employeeId,

                payrollPeriodId.Value,

                cancellationToken);

        }

        else if (fromDate.HasValue && toDate.HasValue)

        {

            var phFrom = PhilippineTime.ToPhilippineDate(fromDate.Value);
            var phTo = PhilippineTime.ToPhilippineDate(toDate.Value);

            payslip = await _reportService.GetPayslipAsync(employeeId, phFrom, phTo, cancellationToken);

        }

        else

        {

            return BadRequest("Select a saved payroll period.");

        }

        if (payslip is null)

        {

            return NotFound();

        }



        var pdf = _payslipPdfService.Generate(payslip);
        var fileName = PayslipPdfService.BuildSinglePayslipFileName(payslip);
        return File(pdf, "application/pdf", fileName);

    }

    [HttpPost("payslip/email")]
    public async Task<ActionResult<ApiResult<object>>> SendPayslipEmail(
        [FromQuery] int employeeId,
        [FromQuery] int payrollPeriodId,
        CancellationToken cancellationToken)
    {
        try
        {
            if (employeeId <= 0 || payrollPeriodId <= 0)
            {
                return BadRequest(ApiResult<object>.Fail("Employee id and payroll period id are required."));
            }

            // Fast pre-checks so the UI gets immediate feedback for missing data,
            // while PDF generation + SMTP run in Hangfire.
            var summary = await _reportService.GetPayrollSummaryByPeriodIdAsync(
                payrollPeriodId,
                cancellationToken);

            var record = summary.Records.FirstOrDefault(r => r.EmployeeId == employeeId);
            if (record is null)
            {
                return NotFound(ApiResult<object>.Fail(
                    "Payslip not found for the selected employee and payroll period."));
            }

            if (record.PayslipEmailSent)
            {
                return BadRequest(ApiResult<object>.Fail(
                    "Payslip already emailed for this payroll period."));
            }

            if (string.IsNullOrWhiteSpace(record.Email))
            {
                return BadRequest(ApiResult<object>.Fail("Missing Email Address"));
            }

            var jobId = _backgroundJobs.Enqueue<PayslipEmailJobs>(
                job => job.SendPayslipEmailAsync(employeeId, payrollPeriodId));

            return Ok(ApiResult<object>.Ok(
                new { JobId = jobId },
                "Payslip email has been queued and will be sent shortly."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResult<object>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResult<object>.Fail(ex.Message));
        }
        catch (Exception)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResult<object>.Fail("Failed to queue payslip email."));
        }
    }

    [HttpGet("payslip/pdf/all")]
    public async Task<IActionResult> GetAllPayslipsPdf(
        [FromQuery] int payrollPeriodId,
        CancellationToken cancellationToken)
    {
        try
        {
            var payslips = await _reportService.GetAllPayslipsByPeriodIdAsync(payrollPeriodId, cancellationToken);
            if (payslips.Count == 0)
            {
                return NotFound("No payslips found for this payroll period.");
            }

            var pdf = _payslipPdfService.GenerateAll(payslips);
            var fileName = PayslipPdfService.BuildAllPayslipsFileName(payslips[0].Period);
            return File(pdf, "application/pdf", fileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }



    [HttpGet("employee-history/{employeeId:int}")]

    public async Task<ActionResult<ApiResult<EmployeePayrollHistoryDto>>> GetEmployeeHistory(

        int employeeId,

        CancellationToken cancellationToken)

    {

        var history = await _reportService.GetEmployeeHistoryAsync(employeeId, cancellationToken);

        if (history is null)

        {

            return NotFound(ApiResult<EmployeePayrollHistoryDto>.Fail("Employee not found."));

        }



        return Ok(ApiResult<EmployeePayrollHistoryDto>.Ok(history));

    }



    [HttpGet("export/payroll-summary")]

    public async Task<IActionResult> ExportPayrollSummaryCsv(

        [FromQuery] DateTime fromDate,

        [FromQuery] DateTime toDate,

        CancellationToken cancellationToken)

    {

        try

        {

            var phFrom = PhilippineTime.ToPhilippineDate(fromDate);
            var phTo = PhilippineTime.ToPhilippineDate(toDate);

            var csv = await _reportService.ExportPayrollSummaryCsvAsync(phFrom, phTo, cancellationToken);

            var fileName = $"PayrollSummary_{phFrom:yyyyMMdd}_{phTo:yyyyMMdd}.csv";

            return File(csv, "text/csv", fileName);

        }

        catch (ArgumentException ex)

        {

            return BadRequest(ex.Message);

        }

    }



    [HttpGet("export/employees")]

    public async Task<IActionResult> ExportEmployeesCsv(

        [FromQuery] EmployeeFilterDto? filter,

        CancellationToken cancellationToken)

    {

        var csv = await _reportService.ExportEmployeesCsvAsync(filter, cancellationToken);

        return File(csv, "text/csv", "Employees.csv");

    }

}


