using Central.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Central.Controllers;

// API interna (sin login de cookie, protegida por una clave) para que el portal
// PHP pueda pedir el archivo de una version. Ruta: GET /Api/Archivo/{id}
public class ApiController : Controller
{
    private readonly CentralDbContext _db;
    private readonly IConfiguration _config;

    public ApiController(CentralDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpGet]
    public async Task<IActionResult> Archivo(int id)
    {
        var claveEsperada = _config["ApiInterna:Clave"] ?? "";
        var claveRecibida = Request.Headers["X-Api-Key"].ToString();
        if (string.IsNullOrEmpty(claveEsperada) || claveRecibida != claveEsperada)
            return Unauthorized();

        var v = await _db.Versiones.FirstOrDefaultAsync(x => x.IdVersion == id);
        if (v == null) return NotFound();

        return File(v.Archivo, DocumentosController.ContentTypePorExtension(v.Extension));
    }
}
