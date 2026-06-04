using Central.Data;
using Central.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Central.Controllers;

[Authorize(Roles = "Admin")]   // solo el Admin
public class EmpresasController : Controller
{
    private readonly CentralDbContext _db;
    public EmpresasController(CentralDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var empresas = await _db.Empresas.OrderBy(e => e.Nombre).ToListAsync();
        return View(empresas);
    }

    [HttpGet]
    public IActionResult Create() => View(new EmpresaFormViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(EmpresaFormViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        bool existe = await _db.Empresas.AnyAsync(e => e.Nombre == vm.Nombre);
        if (existe)
        {
            vm.Error = "Ya existe una empresa con ese nombre.";
            return View(vm);
        }

        _db.Empresas.Add(new Empresa
        {
            Nombre = vm.Nombre,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> ToggleActivo(int id)
    {
        var e = await _db.Empresas.FindAsync(id);
        if (e != null)
        {
            e.Activo = !e.Activo;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction("Index");
    }
}
