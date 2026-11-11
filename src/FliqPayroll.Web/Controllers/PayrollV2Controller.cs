using FliqPayroll.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FliqPayroll.Web.Controllers;

public class PayrollV2Controller : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Payroll";

        return View(new PayrollV2PageViewModel());
    }
}
