using Central.Data;
using Central.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Central.Controllers;

public class HomeController : Controller
{
    private readonly CentralDbContext _db;

    public HomeController(CentralDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var vm = new HomeViewModel();
        try
        {
            vm.Roles = await _db.Roles.OrderBy(r => r.Nivel).ToListAsync();
            vm.Usuarios = await _db.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Empresa)
                .OrderBy(u => u.IdUsuario)
                .ToListAsync();
            vm.Conectado = true;
        }
        catch (Exception ex)
        {
            vm.Conectado = false;
            vm.Error = ex.Message;
        }
        return View(vm);
    }
}
