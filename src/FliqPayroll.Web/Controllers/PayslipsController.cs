using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FliqPayroll.Web.Controllers;

[AllowAnonymous]
public class PayslipsController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Payslips";
        return View();
    }
}
