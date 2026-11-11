using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FliqPayroll.Web.Controllers;

public class PayrollController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Payroll";
        return View();
    }
}
