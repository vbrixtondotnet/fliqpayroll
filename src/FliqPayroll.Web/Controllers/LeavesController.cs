using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FliqPayroll.Web.Controllers;

[AllowAnonymous]
public class LeavesController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Leaves";
        return View();
    }
}
