using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FliqPayroll.Web.Controllers;

[AllowAnonymous]
public class AttendanceController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Attendance Sheet";
        return View();
    }
}
