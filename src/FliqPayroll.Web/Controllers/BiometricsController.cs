using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FliqPayroll.Web.Controllers;

public class BiometricsController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Biometrics";
        return View();
    }
}
