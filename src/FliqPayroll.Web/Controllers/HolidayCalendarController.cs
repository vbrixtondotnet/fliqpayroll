using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FliqPayroll.Web.Controllers;

[AllowAnonymous]
public class HolidayCalendarController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Holiday Calendar";
        return View();
    }
}
