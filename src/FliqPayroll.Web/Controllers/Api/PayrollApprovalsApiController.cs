using FliqPayroll.Core.Constants;
using FliqPayroll.Core.DTOs;
using FliqPayroll.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FliqPayroll.Web.Controllers.Api;

/// <summary>
/// Review workflow for manual edits to generated payroll. Anyone can read the audit trail;
/// only approvers (<see cref="RoleConstants.PayrollApprovers"/>) can approve or reject.
/// </summary>
[ApiController]
[Route("api/payroll-approvals")]
public class PayrollApprovalsApiController : ControllerBase
{
    private readonly IPayrollService _payrollService;

    public PayrollApprovalsApiController(IPayrollService payrollService)
    {
        _payrollService = payrollService;
    }

    /// <summary>Full audit trail for a payroll period (all statuses), newest first.</summary>
    [HttpGet("period/{payrollPeriodId:int}")]
    public async Task<ActionResult<ApiResult<IReadOnlyList<PayrollChangeRequestDto>>>> GetAuditTrail(
        int payrollPeriodId,
        [FromQuery] bool pendingOnly,
        CancellationToken cancellationToken)
    {
        var changes = await _payrollService.GetChangeRequestsAsync(payrollPeriodId, pendingOnly, cancellationToken);
        return Ok(ApiResult<IReadOnlyList<PayrollChangeRequestDto>>.Ok(changes));
    }

    /// <summary>Payroll periods that currently have edits waiting for approval.</summary>
    [HttpGet("pending-periods")]
    [Authorize(Roles = RoleConstants.PayrollApprovers)]
    public async Task<ActionResult<ApiResult<IReadOnlyList<PayrollApprovalSummaryDto>>>> GetPendingPeriods(
        CancellationToken cancellationToken)
    {
        var periods = await _payrollService.GetPendingApprovalSummariesAsync(cancellationToken);
        return Ok(ApiResult<IReadOnlyList<PayrollApprovalSummaryDto>>.Ok(periods));
    }

    [HttpPost("period/{payrollPeriodId:int}/approve-all")]
    [Authorize(Roles = RoleConstants.PayrollApprovers)]
    public async Task<ActionResult<ApiResult<BulkReviewResultDto>>> ApproveAll(
        int payrollPeriodId,
        [FromBody] ReviewPayrollChangeRequestDto? body,
        CancellationToken cancellationToken)
    {
        var count = await _payrollService.ReviewAllPendingAsync(payrollPeriodId, true, ReviewerName, body?.Note, cancellationToken);
        return Ok(ApiResult<BulkReviewResultDto>.Ok(
            new BulkReviewResultDto { ReviewedCount = count },
            $"{count} change(s) approved and applied to the payroll."));
    }

    [HttpPost("period/{payrollPeriodId:int}/reject-all")]
    [Authorize(Roles = RoleConstants.PayrollApprovers)]
    public async Task<ActionResult<ApiResult<BulkReviewResultDto>>> RejectAll(
        int payrollPeriodId,
        [FromBody] ReviewPayrollChangeRequestDto? body,
        CancellationToken cancellationToken)
    {
        var count = await _payrollService.ReviewAllPendingAsync(payrollPeriodId, false, ReviewerName, body?.Note, cancellationToken);
        return Ok(ApiResult<BulkReviewResultDto>.Ok(
            new BulkReviewResultDto { ReviewedCount = count },
            $"{count} change(s) rejected. The payroll keeps its previously approved values."));
    }

    [HttpPost("{changeRequestId:int}/approve")]
    [Authorize(Roles = RoleConstants.PayrollApprovers)]
    public Task<ActionResult<ApiResult<PayrollChangeRequestDto>>> Approve(
        int changeRequestId,
        [FromBody] ReviewPayrollChangeRequestDto? body,
        CancellationToken cancellationToken) =>
        ReviewAsync(
            () => _payrollService.ApproveChangeAsync(changeRequestId, ReviewerName, body?.Note, cancellationToken),
            "Changes approved and applied to the payroll.");

    [HttpPost("{changeRequestId:int}/reject")]
    [Authorize(Roles = RoleConstants.PayrollApprovers)]
    public Task<ActionResult<ApiResult<PayrollChangeRequestDto>>> Reject(
        int changeRequestId,
        [FromBody] ReviewPayrollChangeRequestDto? body,
        CancellationToken cancellationToken) =>
        ReviewAsync(
            () => _payrollService.RejectChangeAsync(changeRequestId, ReviewerName, body?.Note, cancellationToken),
            "Changes rejected. The payroll keeps its previously approved values.");

    private string ReviewerName => User.Identity?.Name ?? "Unknown";

    private async Task<ActionResult<ApiResult<PayrollChangeRequestDto>>> ReviewAsync(
        Func<Task<PayrollChangeRequestDto>> action,
        string successMessage)
    {
        try
        {
            var result = await action();
            return Ok(ApiResult<PayrollChangeRequestDto>.Ok(result, successMessage));
        }
        catch (ArgumentException ex)
        {
            return NotFound(ApiResult<PayrollChangeRequestDto>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResult<PayrollChangeRequestDto>.Fail(ex.Message));
        }
    }
}
