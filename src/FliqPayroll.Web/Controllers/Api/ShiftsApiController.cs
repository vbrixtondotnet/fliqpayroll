using FliqPayroll.Core.DTOs;
using FliqPayroll.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FliqPayroll.Web.Controllers.Api;

// Shifts are fixed (Day / Night): there is intentionally no create or delete endpoint.
[ApiController]
[Route("api/shifts")]
public class ShiftsApiController : ControllerBase
{
    private readonly IShiftService _shiftService;

    public ShiftsApiController(IShiftService shiftService)
    {
        _shiftService = shiftService;
    }

    [HttpGet("getAll")]
    public async Task<ActionResult<ApiResult<IReadOnlyList<ShiftDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var shifts = await _shiftService.GetAllAsync(cancellationToken);
        return Ok(ApiResult<IReadOnlyList<ShiftDto>>.Ok(shifts));
    }

    [HttpGet("employees")]
    public async Task<ActionResult<ApiResult<IReadOnlyList<ShiftEmployeeDto>>>> GetEmployees(CancellationToken cancellationToken)
    {
        var employees = await _shiftService.GetAssignableEmployeesAsync(cancellationToken);
        return Ok(ApiResult<IReadOnlyList<ShiftEmployeeDto>>.Ok(employees));
    }

    [HttpPut("update/{id:int}")]
    public async Task<ActionResult<ApiResult<ShiftDto>>> Update(int id, [FromBody] UpdateShiftDto dto, CancellationToken cancellationToken)
    {
        if (dto is null)
        {
            return BadRequest(ApiResult<ShiftDto>.Fail("Request body is required."));
        }

        try
        {
            var shift = await _shiftService.UpdateAsync(id, dto, cancellationToken);
            return shift is null
                ? NotFound(ApiResult<ShiftDto>.Fail("Shift not found."))
                : Ok(ApiResult<ShiftDto>.Ok(shift, "Shift updated."));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(ApiResult<ShiftDto>.Fail(ex.Message));
        }
    }

    [HttpPut("{id:int}/employees")]
    public async Task<ActionResult<ApiResult<ShiftDto>>> SetEmployees(int id, [FromBody] AssignShiftEmployeesDto dto, CancellationToken cancellationToken)
    {
        if (dto is null)
        {
            return BadRequest(ApiResult<ShiftDto>.Fail("Request body is required."));
        }

        var shift = await _shiftService.SetEmployeesAsync(id, dto, cancellationToken);
        return shift is null
            ? NotFound(ApiResult<ShiftDto>.Fail("Shift not found."))
            : Ok(ApiResult<ShiftDto>.Ok(shift, "Shift employees saved."));
    }
}
