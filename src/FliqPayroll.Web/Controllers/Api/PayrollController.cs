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

        [FromQuery] DateTime fromDate,

        [FromQuery] DateTime toDate,

        CancellationToken cancellationToken)

    {

        try

        {

            var result = await _payrollService.GetByDateRangeAsync(
                PhilippineTime.ToPhilippineDate(fromDate),
                PhilippineTime.ToPhilippineDate(toDate),
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

}


