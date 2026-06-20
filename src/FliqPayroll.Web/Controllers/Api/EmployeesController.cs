using FliqPayroll.Core.DTOs;
using FliqPayroll.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FliqPayroll.Web.Controllers.Api;

[ApiController]
[Route("api/employees")]
[AllowAnonymous]
public class EmployeesApiController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesApiController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResult<IReadOnlyList<EmployeeDto>>>> GetAll(
        [FromQuery] EmployeeFilterDto? filter,
        CancellationToken cancellationToken)
    {
        var employees = await _employeeService.GetAllAsync(filter, cancellationToken);
        return Ok(ApiResult<IReadOnlyList<EmployeeDto>>.Ok(employees));
    }

    [HttpGet("departments")]
    public async Task<ActionResult<ApiResult<IReadOnlyList<string>>>> GetDepartments(CancellationToken cancellationToken)
    {
        var departments = await _employeeService.GetDepartmentsAsync(cancellationToken);
        return Ok(ApiResult<IReadOnlyList<string>>.Ok(departments));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResult<EmployeeDto>>> GetById(int id, CancellationToken cancellationToken)
    {
        var employee = await _employeeService.GetByIdAsync(id, cancellationToken);
        if (employee is null)
        {
            return NotFound(ApiResult<EmployeeDto>.Fail("Employee not found."));
        }

        return Ok(ApiResult<EmployeeDto>.Ok(employee));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResult<EmployeeDto>>> Create(
        [FromBody] CreateEmployeeDto dto,
        CancellationToken cancellationToken)
    {
        if (dto is null)
        {
            return BadRequest(ApiResult<EmployeeDto>.Fail("Request body is required."));
        }

        try
        {
            var created = await _employeeService.CreateAsync(dto, cancellationToken);
            return Ok(ApiResult<EmployeeDto>.Ok(created, "Employee created."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResult<EmployeeDto>.Fail(ex.Message));
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResult<EmployeeDto>>> Update(
        int id,
        [FromBody] UpdateEmployeeDto dto,
        CancellationToken cancellationToken)
    {
        if (dto is null)
        {
            return BadRequest(ApiResult<EmployeeDto>.Fail("Request body is required."));
        }

        dto.Id = id;

        try
        {
            var updated = await _employeeService.UpdateAsync(dto, cancellationToken);
            if (updated is null)
            {
                return NotFound(ApiResult<EmployeeDto>.Fail("Employee not found."));
            }

            return Ok(ApiResult<EmployeeDto>.Ok(updated, "Employee updated."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResult<EmployeeDto>.Fail(ex.Message));
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResult<bool>>> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _employeeService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(ApiResult<bool>.Fail("Employee not found."));
        }

        return Ok(ApiResult<bool>.Ok(true, "Employee deleted."));
    }
}
