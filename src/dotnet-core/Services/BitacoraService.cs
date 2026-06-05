using System.Security.Claims;
using Central.Data;
using Central.Models;
using Microsoft.AspNetCore.Http;

namespace Central.Services;

public class BitacoraService
{
    private readonly CentralDbContext _db;
    private readonly IHttpContextAccessor _http;

    public BitacoraService(CentralDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task RegistrarAsync(string accion, string? entidad = null, int? idEntidad = null,
                                     string? detalle = null, int? idUsuarioOverride = null)
    {
        var ctx = _http.HttpContext;

        int? idUsuario = idUsuarioOverride;
        if (idUsuario == null && ctx?.User?.Identity?.IsAuthenticated == true)
        {
            var claim = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(claim, out var id)) idUsuario = id;
        }

        string? ip = ctx?.Connection?.RemoteIpAddress?.ToString();

        if (detalle != null && detalle.Length > 500)
            detalle = detalle.Substring(0, 500);

        _db.Bitacora.Add(new Bitacora
        {
            IdUsuario = idUsuario,
            Accion = accion,
            Entidad = entidad,
            IdEntidad = idEntidad,
            Detalle = detalle,
            DireccionIP = ip,
            FechaHora = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}
