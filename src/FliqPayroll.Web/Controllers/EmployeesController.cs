using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FliqPayroll.Web.Controllers;

public class EmployeesController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Employees";
        return View();
    }
}
