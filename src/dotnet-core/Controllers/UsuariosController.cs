using Central.Data;
using Central.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Central.Controllers;

[Authorize(Roles = "Admin")]   // solo el Admin
public class UsuariosController : Controller
{
    private readonly CentralDbContext _db;
    public UsuariosController(CentralDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var usuarios = await _db.Usuarios
            .Include(u => u.Rol)
            .Include(u => u.Empresa)
            .OrderBy(u => u.IdUsuario)
            .ToListAsync();
        return View(usuarios);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vm = new UsuarioFormViewModel();
        await PopulateListsAsync(vm);
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Create(UsuarioFormViewModel vm)
    {
        await PopulateListsAsync(vm);

        if (string.IsNullOrWhiteSpace(vm.Contrasena))
            ModelState.AddModelError(nameof(vm.Contrasena), "La contrasena es obligatoria");

        if (!ModelState.IsValid)
            return View(vm);

        if (await _db.Usuarios.AnyAsync(u => u.Correo == vm.Correo))
        {
            vm.Error = "Ya existe un usuario con ese correo.";
            return View(vm);
        }

        int? idEmpresa = await ResolverEmpresaAsync(vm);
        if (idEmpresa == null && !await EsRolAdminAsync(vm.IdRol))
        {
            vm.Error = "Selecciona una empresa para este rol.";
            return View(vm);
        }

        var hasher = new PasswordHasher<Usuario>();
        var usuario = new Usuario
        {
            NombreCompleto = vm.NombreCompleto,
            Correo = vm.Correo,
            IdRol = vm.IdRol,
            IdEmpresa = idEmpresa,
            Activo = true,
            FechaCreacion = DateTime.UtcNow,
            ContrasenaHash = ""
        };
        usuario.ContrasenaHash = hasher.HashPassword(usuario, vm.Contrasena!);

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var u = await _db.Usuarios.FindAsync(id);
        if (u == null) return NotFound();

        var vm = new UsuarioFormViewModel
        {
            IdUsuario = u.IdUsuario,
            NombreCompleto = u.NombreCompleto,
            Correo = u.Correo,
            IdRol = u.IdRol,
            IdEmpresa = u.IdEmpresa,
            Activo = u.Activo
        };
        await PopulateListsAsync(vm);
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(UsuarioFormViewModel vm)
    {
        await PopulateListsAsync(vm);

        if (!ModelState.IsValid)
            return View(vm);

        var usuario = await _db.Usuarios.FindAsync(vm.IdUsuario);
        if (usuario == null) return NotFound();

        if (await _db.Usuarios.AnyAsync(u => u.Correo == vm.Correo && u.IdUsuario != vm.IdUsuario))
        {
            vm.Error = "Ya existe otro usuario con ese correo.";
            return View(vm);
        }

        int? idEmpresa = await ResolverEmpresaAsync(vm);
        if (idEmpresa == null && !await EsRolAdminAsync(vm.IdRol))
        {
            vm.Error = "Selecciona una empresa para este rol.";
            return View(vm);
        }

        usuario.NombreCompleto = vm.NombreCompleto;
        usuario.Correo = vm.Correo;
        usuario.IdRol = vm.IdRol;
        usuario.IdEmpresa = idEmpresa;
        usuario.Activo = vm.Activo;

        if (!string.IsNullOrWhiteSpace(vm.Contrasena))
        {
            var hasher = new PasswordHasher<Usuario>();
            usuario.ContrasenaHash = hasher.HashPassword(usuario, vm.Contrasena);
        }

        await _db.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> ToggleActivo(int id)
    {
        var u = await _db.Usuarios.FindAsync(id);
        if (u != null)
        {
            u.Activo = !u.Activo;
            await _db.SaveChangesAsync();
        }
        return RedirectToAction("Index");
    }

    private async Task PopulateListsAsync(UsuarioFormViewModel vm)
    {
        vm.Roles = await _db.Roles.OrderBy(r => r.Nivel).ToListAsync();
        vm.Empresas = await _db.Empresas.Where(e => e.Activo).OrderBy(e => e.Nombre).ToListAsync();
    }

    private async Task<bool> EsRolAdminAsync(int idRol)
    {
        var rol = await _db.Roles.FindAsync(idRol);
        return rol != null && rol.Nombre == "Admin";
    }

    // Si el rol es Admin, la empresa es null; si no, se respeta la elegida.
    private async Task<int?> ResolverEmpresaAsync(UsuarioFormViewModel vm)
    {
        return await EsRolAdminAsync(vm.IdRol) ? null : vm.IdEmpresa;
    }
}
