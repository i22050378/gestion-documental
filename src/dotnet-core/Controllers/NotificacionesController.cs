using System.Security.Claims;
using Central.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Central.Controllers;

[Authorize]
public class NotificacionesController : Controller
{
    private readonly CentralDbContext _db;
    public NotificacionesController(CentralDbContext db) => _db = db;

    private int UsuarioActual()
    {
        var v = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(v, out var id) ? id : 0;
    }

    public async Task<IActionResult> Index()
    {
        int id = UsuarioActual();
        var lista = await _db.Notificaciones
            .Where(n => n.IdUsuario == id)
            .OrderByDescending(n => n.FechaCreacion)
            .ToListAsync();
        return View(lista);
    }

    // Abrir una notificacion: marcarla leida y, si tiene version, ir al documento
    [HttpGet]
    public async Task<IActionResult> Abrir(int id)
    {
        int usuario = UsuarioActual();
        var n = await _db.Notificaciones
            .FirstOrDefaultAsync(x => x.IdNotificacion == id && x.IdUsuario == usuario);
        if (n == null) return NotFound();

        n.Leida = true;
        await _db.SaveChangesAsync();

        if (n.IdVersion != null)
        {
            var idDoc = await _db.Versiones
                .Where(v => v.IdVersion == n.IdVersion)
                .Select(v => v.IdDocumento)
                .FirstOrDefaultAsync();
            if (idDoc != 0)
                return RedirectToAction("Detalle", "Documentos", new { id = idDoc });
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> MarcarTodas()
    {
        int usuario = UsuarioActual();
        await _db.Notificaciones
            .Where(n => n.IdUsuario == usuario && !n.Leida)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.Leida, true));
        return RedirectToAction("Index");
    }
}
