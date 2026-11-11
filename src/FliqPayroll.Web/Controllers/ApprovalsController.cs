using FliqPayroll.Core.Constants;
using FliqPayroll.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FliqPayroll.Web.Controllers;

/// <summary>Where Finance Officers review and approve manual edits made to generated payroll.</summary>
[Authorize(Roles = RoleConstants.PayrollApprovers)]
public class ApprovalsController : Controller
{
    private readonly IPayrollService _payrollService;

    public ApprovalsController(IPayrollService payrollService)
    {
        _payrollService = payrollService;
    }

    public IActionResult Index()
    {
        ViewData["Title"] = "Approvals";
        return View();
    }

    /// <summary>Landing page of the approval email: all pending changes of one payroll period.</summary>
    public async Task<IActionResult> Period(int id, CancellationToken cancellationToken)
    {
        var period = await _payrollService.GetSavedPeriodByIdAsync(id, cancellationToken);
        if (period is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Approvals";
        ViewData["PeriodId"] = id;
        ViewData["PeriodName"] = period.PeriodName;
        return View();
    }
}
