using FliqPayroll.Core.DTOs;
using FliqPayroll.Core.Utilities;
using FliqPayroll.Services.Interfaces;using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FliqPayroll.Web.Controllers.Api;

[ApiController]
[Route("api/holidays")]
[AllowAnonymous]
public class HolidaysApiController : ControllerBase
{
    private readonly IHolidayService _holidayService;

    public HolidaysApiController(IHolidayService holidayService)
    {
        _holidayService = holidayService;
    }

    [HttpGet("getAll")]
    public async Task<ActionResult<ApiResult<IReadOnlyList<HolidayDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var holidays = await _holidayService.GetAllAsync(cancellationToken);
        return Ok(ApiResult<IReadOnlyList<HolidayDto>>.Ok(holidays));
    }

    [HttpGet("getByDate")]
    public async Task<ActionResult<ApiResult<HolidayDto>>> GetByDate(
        [FromQuery] DateTime date,
        CancellationToken cancellationToken)
    {
        var holiday = await _holidayService.GetByDateAsync(date, cancellationToken);
        if (holiday is null)
        {
            return NotFound(ApiResult<HolidayDto>.Fail("No holiday found for this date."));
        }

        return Ok(ApiResult<HolidayDto>.Ok(holiday));
    }

    [HttpPost("add")]
    public async Task<ActionResult<ApiResult<HolidayDto>>> Add(
        [FromBody] CreateHolidayDto dto,
        CancellationToken cancellationToken)
    {
        if (dto is null)
        {
            return BadRequest(ApiResult<HolidayDto>.Fail("Request body is required."));
        }

        try
        {
            var holiday = await _holidayService.AddAsync(dto, cancellationToken);
            return Ok(ApiResult<HolidayDto>.Ok(holiday, "Holiday saved."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResult<HolidayDto>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResult<HolidayDto>.Fail(ex.Message));
        }
    }

    [HttpPut("update/{id:int}")]
    public async Task<ActionResult<ApiResult<HolidayDto>>> Update(
        int id,
        [FromBody] UpdateHolidayDto dto,
        CancellationToken cancellationToken)
    {
        if (dto is null)
        {
            return BadRequest(ApiResult<HolidayDto>.Fail("Request body is required."));
        }

        try
        {
            var holiday = await _holidayService.UpdateAsync(id, dto, cancellationToken);
            if (holiday is null)
            {
                return NotFound(ApiResult<HolidayDto>.Fail("Holiday not found."));
            }

            return Ok(ApiResult<HolidayDto>.Ok(holiday, "Holiday updated."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResult<HolidayDto>.Fail(ex.Message));
        }
    }

    [HttpDelete("delete/{id:int}")]
    public async Task<ActionResult<ApiResult<bool>>> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _holidayService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(ApiResult<bool>.Fail("Holiday not found."));
        }

        return Ok(ApiResult<bool>.Ok(true, "Holiday deleted."));
    }
}
