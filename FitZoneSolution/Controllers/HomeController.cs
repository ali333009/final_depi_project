using Microsoft.AspNetCore.Mvc;

namespace FitZone.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        ViewData["LandingPage"] = true;
        ViewData["FullPage"] = true;
        ViewData["HideFooter"] = true;
        return View();
    }

    public IActionResult Error()
    {
        return View();
    }
}
