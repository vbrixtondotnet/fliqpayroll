using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FliqPayroll.Web.Controllers;

[AllowAnonymous]
public class BiometricsController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Biometrics";
        return View();
    }
}
