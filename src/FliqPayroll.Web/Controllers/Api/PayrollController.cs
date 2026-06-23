using FliqPayroll.Core.DTOs;
using FliqPayroll.Core.Utilities;
using FliqPayroll.Services.Interfaces;

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;



namespace FliqPayroll.Web.Controllers.Api;



[ApiController]

[Route("api/payroll")]

[AllowAnonymous]

public class PayrollApiController : ControllerBase

{

    private readonly IPayrollService _payrollService;



    public PayrollApiController(IPayrollService payrollService)

    {

        _payrollService = payrollService;

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
                    Records = request.Records
                },
                cancellationToken);

            return Ok(ApiResult<SavePayrollPeriodResultDto>.Ok(result, "Payroll period saved successfully."));
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

}


