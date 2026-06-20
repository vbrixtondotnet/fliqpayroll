using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FliqPayroll.Web.Controllers;

[AllowAnonymous]
public class PayrollController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Payroll";
        return View();
    }
}
