using FliqPayroll.Core.DTOs;
using FliqPayroll.Core.Utilities;
using FliqPayroll.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FliqPayroll.Web.Controllers.Api;

[ApiController]
[Route("api/leaves")]
[AllowAnonymous]
public class LeavesApiController : ControllerBase
{
    private readonly ILeaveService _leaveService;

    public LeavesApiController(ILeaveService leaveService)
    {
        _leaveService = leaveService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResult<IReadOnlyList<LeaveDto>>>> GetByDateRange(
        [FromQuery] string? fromDate,
        [FromQuery] string? toDate,
        CancellationToken cancellationToken)
    {
        if (!PhilippineTime.TryParseCalendarDate(fromDate, out var parsedFromDate) ||
            !PhilippineTime.TryParseCalendarDate(toDate, out var parsedToDate))
        {
            return BadRequest(ApiResult<IReadOnlyList<LeaveDto>>.Fail("Invalid date format. Use YYYY-MM-DD for From and To."));
        }

        if (parsedToDate < parsedFromDate)
        {
            return BadRequest(ApiResult<IReadOnlyList<LeaveDto>>.Fail("To date must be on or after From date."));
        }

        try
        {
            var leaves = await _leaveService.GetByDateRangeAsync(parsedFromDate, parsedToDate, cancellationToken);
            return Ok(ApiResult<IReadOnlyList<LeaveDto>>.Ok(leaves));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResult<IReadOnlyList<LeaveDto>>.Fail(ex.Message));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResult<LeaveDto>>> Create(
        [FromBody] CreateLeaveDto? dto,
        CancellationToken cancellationToken)
    {
        if (dto is null)
        {
            return BadRequest(ApiResult<LeaveDto>.Fail("Request body is required."));
        }

        try
        {
            var leave = await _leaveService.CreateAsync(dto, cancellationToken);
            return Ok(ApiResult<LeaveDto>.Ok(leave, "Leave record saved."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResult<LeaveDto>.Fail(ex.Message));
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResult<bool>>> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _leaveService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(ApiResult<bool>.Fail("Leave record not found."));
        }

        return Ok(ApiResult<bool>.Ok(true, "Leave record deleted."));
    }
}
