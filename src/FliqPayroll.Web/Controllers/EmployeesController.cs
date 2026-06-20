using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FliqPayroll.Web.Controllers;

[AllowAnonymous]
public class EmployeesController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Employees";
        return View();
    }
}
