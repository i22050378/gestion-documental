using System.Security.Claims;
using Central.Data;
using Central.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Central.Controllers;

[Authorize]
public class BitacoraController : Controller
{
    private readonly CentralDbContext _db;
    public BitacoraController(CentralDbContext db) => _db = db;

    private bool EsAdmin => User.IsInRole("Admin");

    private int? EmpresaActual()
    {
        var v = User.FindFirst("IdEmpresa")?.Value;
        return int.TryParse(v, out var id) ? id : (int?)null;
    }

    private int UsuarioActual()
    {
        var v = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(v, out var id) ? id : 0;
    }

    public async Task<IActionResult> Index()
    {
        var usuarios = await _db.Usuarios.ToDictionaryAsync(u => u.IdUsuario, u => u.NombreCompleto);

        IQueryable<Bitacora> q = _db.Bitacora;

        if (EsAdmin)
        {
            // Admin ve todo
        }
        else if (User.IsInRole("Director"))
        {
            var empresa = EmpresaActual();
            var idsEmpresa = await _db.Usuarios
                .Where(u => u.IdEmpresa == empresa)
                .Select(u => u.IdUsuario)
                .ToListAsync();
            q = q.Where(b => b.IdUsuario.HasValue && idsEmpresa.Contains(b.IdUsuario.Value));
        }
        else
        {
            int yo = UsuarioActual();
            q = q.Where(b => b.IdUsuario == yo);
        }

        var entradas = await q
            .OrderByDescending(b => b.FechaHora)
            .Take(300)
            .Select(b => new BitacoraRow
            {
                FechaHora = b.FechaHora,
                IdUsuario = b.IdUsuario,
                Accion = b.Accion,
                Entidad = b.Entidad,
                IdEntidad = b.IdEntidad,
                Detalle = b.Detalle,
                DireccionIP = b.DireccionIP
            })
            .ToListAsync();

        foreach (var e in entradas)
            e.Usuario = e.IdUsuario.HasValue && usuarios.ContainsKey(e.IdUsuario.Value)
                ? usuarios[e.IdUsuario.Value]
                : "(anonimo)";

        ViewData["EsAdmin"] = EsAdmin;
        return View(entradas);
    }
}
