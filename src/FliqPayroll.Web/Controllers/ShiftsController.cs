using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FliqPayroll.Web.Controllers;

public class ShiftsController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Shifts";
        return View();
    }
}
