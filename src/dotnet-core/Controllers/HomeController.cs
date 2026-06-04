using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Central.Controllers;

[Authorize]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        ViewData["Nombre"] = User.Identity?.Name;
        ViewData["Rol"] = User.FindFirst(ClaimTypes.Role)?.Value;
        ViewData["Empresa"] = User.FindFirst("Empresa")?.Value;
        return View();
    }
}
