using System.Security.Claims;
using Central.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Central.ViewComponents;

public class NotificacionesBadgeViewComponent : ViewComponent
{
    private readonly CentralDbContext _db;
    public NotificacionesBadgeViewComponent(CentralDbContext db) => _db = db;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        int idUsuario = 0;
        var claim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(claim, out idUsuario);

        int noLeidas = idUsuario == 0
            ? 0
            : await _db.Notificaciones.CountAsync(n => n.IdUsuario == idUsuario && !n.Leida);

        return View(noLeidas);
    }
}
