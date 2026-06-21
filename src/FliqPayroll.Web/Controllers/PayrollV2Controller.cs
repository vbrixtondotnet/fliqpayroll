using FliqPayroll.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FliqPayroll.Web.Controllers;

[AllowAnonymous]
public class PayrollV2Controller : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Payroll v2";

        return View(new PayrollV2PageViewModel());
    }
}
