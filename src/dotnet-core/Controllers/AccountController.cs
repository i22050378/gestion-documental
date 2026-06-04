using System.Security.Claims;
using Central.Data;
using Central.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Central.Controllers;

public class AccountController : Controller
{
    private readonly CentralDbContext _db;
    public AccountController(CentralDbContext db) => _db = db;

    [HttpGet]
    public IActionResult Login() => View(new LoginViewModel());

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var usuario = await _db.Usuarios
            .Include(u => u.Rol)
            .Include(u => u.Empresa)
            .FirstOrDefaultAsync(u => u.Correo == model.Correo && u.Activo);

        var hasher = new PasswordHasher<Usuario>();
        bool ok = usuario != null &&
                  hasher.VerifyHashedPassword(usuario, usuario.ContrasenaHash, model.Contrasena)
                      != PasswordVerificationResult.Failed;

        if (!ok)
        {
            model.Contrasena = "";
            model.Error = "Correo o contrasena incorrectos.";
            return View(model);
        }

        usuario!.UltimoAcceso = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
            new(ClaimTypes.Name, usuario.NombreCompleto),
            new(ClaimTypes.Email, usuario.Correo),
            new(ClaimTypes.Role, usuario.Rol?.Nombre ?? ""),
            new("IdEmpresa", usuario.IdEmpresa?.ToString() ?? ""),
            new("Empresa", usuario.Empresa?.Nombre ?? "(global)")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}
