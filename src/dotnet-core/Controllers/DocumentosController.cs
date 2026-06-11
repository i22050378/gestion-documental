using System.Security.Claims;
using Central.Data;
using Central.Models;
using Central.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Central.Controllers;

[Authorize]
public class DocumentosController : Controller
{
    private readonly CentralDbContext _db;
    private readonly BitacoraService _bitacora;
    private readonly IndexacionClient _indexacion;
    private readonly ConsultaClient _consulta;
    public DocumentosController(CentralDbContext db, BitacoraService bitacora, IndexacionClient indexacion, ConsultaClient consulta)
    {
        _db = db;
        _bitacora = bitacora;
        _indexacion = indexacion;
        _consulta = consulta;
    }

    private static readonly string[] ExtensionesPermitidas =
        { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".png", ".jpg", ".jpeg", ".txt", ".dwg" };

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

    private async Task<int> EstadoIdAsync(string nombre) =>
        await _db.Estados.Where(e => e.Nombre == nombre).Select(e => e.IdEstado).FirstAsync();

    // Devuelve los IdUsuario activos que tienen cierto rol dentro de una empresa.
    // Sirve para avisar (notificar) al Revisor o al Aprobador segun el paso del flujo.
    private async Task<List<int>> IdsUsuariosPorRolAsync(string rol, int? idEmpresa) =>
        await _db.Usuarios
            .Where(u => u.Activo && u.IdEmpresa == idEmpresa && u.Rol != null && u.Rol.Nombre == rol)
            .Select(u => u.IdUsuario)
            .ToListAsync();

    // Crea una notificacion para cada usuario de la lista.
    private void NotificarUsuarios(IEnumerable<int> idsUsuarios, string mensaje, int idVersion)
    {
        foreach (var uid in idsUsuarios)
            _db.Notificaciones.Add(new Notificacion
            {
                IdUsuario = uid,
                Mensaje = mensaje,
                IdVersion = idVersion,
                Leida = false,
                FechaCreacion = DateTime.UtcNow
            });
    }

    // ---------- Lista ----------
    public async Task<IActionResult> Index()
    {
        var idEmpresa = EmpresaActual();
        var esAdmin = EsAdmin;

        var estados = await _db.Estados.ToDictionaryAsync(e => e.IdEstado, e => e.Nombre);

        var docs = await _db.Documentos
            .Where(d => d.Activo && (esAdmin || d.IdEmpresa == idEmpresa))
            .OrderByDescending(d => d.IdDocumento)
            .Select(d => new DocumentoListItem
            {
                IdDocumento = d.IdDocumento,
                Titulo = d.Titulo,
                Categoria = d.Categoria!.Nombre,
                Creador = d.Creador!.NombreCompleto,
                Empresa = d.Empresa!.Nombre,
                FechaCreacion = d.FechaCreacion,
                UltimaVersion = d.Versiones.OrderByDescending(v => v.NumeroVersion)
                                           .Select(v => v.NumeroVersion).FirstOrDefault(),
                UltimaVersionMajor = d.Versiones.OrderByDescending(v => v.NumeroVersion)
                                                .Select(v => v.VersionMajor).FirstOrDefault(),
                UltimaVersionMinor = d.Versiones.OrderByDescending(v => v.NumeroVersion)
                                                .Select(v => v.VersionMinor).FirstOrDefault(),
                IdEstadoUltima = d.Versiones.OrderByDescending(v => v.NumeroVersion)
                                            .Select(v => v.IdEstado).FirstOrDefault(),
                IdUltimaVersion = d.Versiones.OrderByDescending(v => v.NumeroVersion)
                                             .Select(v => v.IdVersion).FirstOrDefault()
            })
            .ToListAsync();

        foreach (var d in docs)
            d.Estado = estados.GetValueOrDefault(d.IdEstadoUltima, "");

        ViewData["EsAdmin"] = esAdmin;
        return View(docs);
    }

    // ---------- Detalle (info + versiones + revisiones + acciones) ----------
    public async Task<IActionResult> Detalle(int id)
    {
        var idEmpresa = EmpresaActual();
        var esAdmin = EsAdmin;

        var doc = await _db.Documentos
            .Where(d => d.IdDocumento == id && d.Activo)
            .Select(d => new
            {
                d.IdDocumento,
                d.IdEmpresa,
                d.Titulo,
                d.Descripcion,
                d.FechaCreacion,
                Categoria = d.Categoria!.Nombre,
                Empresa = d.Empresa!.Nombre,
                Creador = d.Creador!.NombreCompleto
            })
            .FirstOrDefaultAsync();

        if (doc == null) return NotFound();
        if (!esAdmin && doc.IdEmpresa != idEmpresa) return Forbid();

        var estados = await _db.Estados.ToDictionaryAsync(e => e.IdEstado, e => e.Nombre);
        var usuarios = await _db.Usuarios.ToDictionaryAsync(u => u.IdUsuario, u => u.NombreCompleto);

        var versionesRaw = await _db.Versiones
            .Where(v => v.IdDocumento == id)
            .OrderByDescending(v => v.NumeroVersion)
            .Select(v => new { v.IdVersion, v.NumeroVersion, v.VersionMajor, v.VersionMinor, v.IdEstado, v.IdUsuarioSubio, v.FechaSubida, v.TamanoBytes, v.Extension })
            .ToListAsync();

        var versionIds = versionesRaw.Select(v => v.IdVersion).ToList();

        var revisionesRaw = await _db.Revisiones
            .Where(r => versionIds.Contains(r.IdVersion))
            .OrderByDescending(r => r.FechaHora)
            .Select(r => new { r.Tipo, r.Comentario, r.IdUsuario, r.FechaHora })
            .ToListAsync();

        var vm = new DocumentoDetalleViewModel
        {
            IdDocumento = doc.IdDocumento,
            Titulo = doc.Titulo,
            Descripcion = doc.Descripcion,
            Categoria = doc.Categoria,
            Empresa = doc.Empresa,
            Creador = doc.Creador,
            FechaCreacion = doc.FechaCreacion
        };

        foreach (var v in versionesRaw)
        {
            var nombreEstado = estados.GetValueOrDefault(v.IdEstado, "");
            vm.Versiones.Add(new VersionRow
            {
                IdVersion = v.IdVersion,
                NumeroVersion = v.NumeroVersion,
                VersionMajor = v.VersionMajor,
                VersionMinor = v.VersionMinor,
                Estado = nombreEstado,
                Extension = v.Extension,
                SubioPor = usuarios.GetValueOrDefault(v.IdUsuarioSubio, ""),
                FechaSubida = v.FechaSubida,
                TamanoBytes = v.TamanoBytes,
                EsVigente = nombreEstado == "Aprobado"
            });
        }

        foreach (var r in revisionesRaw)
        {
            vm.Revisiones.Add(new RevisionRow
            {
                Tipo = r.Tipo,
                Comentario = r.Comentario,
                Usuario = usuarios.GetValueOrDefault(r.IdUsuario, ""),
                FechaHora = r.FechaHora
            });
        }

        var ultima = vm.Versiones.FirstOrDefault();  // mayor NumeroVersion
        if (ultima != null)
        {
            vm.IdUltimaVersion = ultima.IdVersion;
            vm.EstadoUltima = ultima.Estado;
        }

        bool mismaEmpresa = doc.IdEmpresa == idEmpresa;
        vm.PuedeRevisar = User.IsInRole("Revisor") && mismaEmpresa && vm.EstadoUltima == "Pendiente de revision";
        vm.PuedeAprobar = User.IsInRole("Aprobador") && mismaEmpresa && vm.EstadoUltima == "Pendiente de aprobacion";
        vm.PuedeSubirNueva = User.IsInRole("Supervisor")
                             && mismaEmpresa && vm.EstadoUltima == "Rechazado";
        vm.PuedeReportarError = mismaEmpresa && vm.EstadoUltima == "Aprobado";
        vm.PuedePrerrevisar = (User.IsInRole("Empleado") || User.IsInRole("Supervisor"))
                              && mismaEmpresa && vm.EstadoUltima == "Pendiente de revision";

        await _bitacora.RegistrarAsync("ABRIR", "DOCUMENTO", vm.IdDocumento, vm.Titulo);
        return View(vm);
    }

    // ---------- Crear documento (version 1) ----------
    [HttpGet]
    [Authorize(Roles = "Supervisor")]
    public async Task<IActionResult> Create()
    {
        var vm = new DocumentoFormViewModel
        {
            Categorias = await _db.Categorias.OrderBy(c => c.Nombre).ToListAsync()
        };
        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Supervisor")]
    public async Task<IActionResult> Create(DocumentoFormViewModel vm)
    {
        vm.Categorias = await _db.Categorias.OrderBy(c => c.Nombre).ToListAsync();

        if (vm.Archivo == null || vm.Archivo.Length == 0)
            ModelState.AddModelError(nameof(vm.Archivo), "Selecciona un archivo");

        if (!ModelState.IsValid)
            return View(vm);

        var extension = Path.GetExtension(vm.Archivo!.FileName).ToLowerInvariant();
        if (!ExtensionesPermitidas.Contains(extension))
        {
            vm.Error = "Tipo de archivo no permitido. Permitidos: " + string.Join(", ", ExtensionesPermitidas);
            return View(vm);
        }

        var idEmpresa = EmpresaActual();
        if (idEmpresa == null)
        {
            vm.Error = "Tu usuario no tiene una empresa asignada.";
            return View(vm);
        }

        var idPendiente = await EstadoIdAsync("Pendiente de revision");

        using var ms = new MemoryStream();
        await vm.Archivo.CopyToAsync(ms);
        var bytes = ms.ToArray();
        int usuario = UsuarioActual();

        var documento = new Documento
        {
            IdEmpresa = idEmpresa.Value,
            IdCategoria = vm.IdCategoria,
            IdUsuarioCreador = usuario,
            Titulo = vm.Titulo,
            Descripcion = vm.Descripcion,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        documento.Versiones.Add(new DocumentoVersion
        {
            NumeroVersion = 1,
            VersionMajor = 0,
            VersionMinor = 1,
            IdEstado = idPendiente,
            IdUsuarioSubio = usuario,
            NombreArchivo = Path.GetFileName(vm.Archivo.FileName),
            Extension = extension,
            TamanoBytes = bytes.LongLength,
            Archivo = bytes,
            FechaSubida = DateTime.UtcNow,
            Activo = true
        });

        _db.Documentos.Add(documento);
        await _db.SaveChangesAsync();

        // Avisar a los Revisores de la empresa que hay un documento nuevo por revisar.
        var versionCreada = documento.Versiones.First();
        var revisores = await IdsUsuariosPorRolAsync("Revisor", documento.IdEmpresa);
        NotificarUsuarios(revisores,
            $"Hay un documento nuevo por revisar: \"{documento.Titulo}\" (v{versionCreada.VersionMajor}.{versionCreada.VersionMinor}).",
            versionCreada.IdVersion);
        await _db.SaveChangesAsync();

        await _bitacora.RegistrarAsync("SUBIR", "DOCUMENTO", documento.IdDocumento, documento.Titulo);
        return RedirectToAction("Index");
    }

    // ---------- Revisar: el Revisor PASA el documento al Aprobador ----------
    [HttpPost]
    [Authorize(Roles = "Revisor")]
    public async Task<IActionResult> RevisarPasar(int idVersion)
    {
        var version = await _db.Versiones.FirstOrDefaultAsync(v => v.IdVersion == idVersion);
        if (version == null) return NotFound();

        var doc = await _db.Documentos.FirstOrDefaultAsync(d => d.IdDocumento == version.IdDocumento);
        if (doc == null) return NotFound();
        if (doc.IdEmpresa != EmpresaActual()) return Forbid();

        var idPendienteRev = await EstadoIdAsync("Pendiente de revision");
        if (version.IdEstado != idPendienteRev)
            return RedirectToAction("Detalle", new { id = version.IdDocumento });

        // Pasa a esperar la aprobacion final (no cambia el numero de version todavia).
        version.IdEstado = await EstadoIdAsync("Pendiente de aprobacion");
        version.IdUsuarioRevisor = UsuarioActual();
        version.FechaRevision = DateTime.UtcNow;

        _db.Revisiones.Add(new Revision
        {
            IdVersion = version.IdVersion,
            IdUsuario = UsuarioActual(),
            Tipo = "REVISION_OK",
            Comentario = null,
            FechaHora = DateTime.UtcNow
        });

        // Avisar a los Aprobadores de la empresa que hay un documento por aprobar.
        var aprobadores = await IdsUsuariosPorRolAsync("Aprobador", doc.IdEmpresa);
        NotificarUsuarios(aprobadores,
            $"El documento \"{doc.Titulo}\" (v{version.VersionMajor}.{version.VersionMinor}) paso la revision y espera tu aprobacion.",
            version.IdVersion);

        await _db.SaveChangesAsync();
        await _bitacora.RegistrarAsync("REVISADO", "VERSION", version.IdVersion, doc.Titulo);
        return RedirectToAction("Detalle", new { id = version.IdDocumento });
    }

    // ---------- Revisar: el Revisor RECHAZA el documento ----------
    [HttpPost]
    [Authorize(Roles = "Revisor")]
    public async Task<IActionResult> RevisarRechazar(int idVersion, string? comentario)
    {
        var version = await _db.Versiones.FirstOrDefaultAsync(v => v.IdVersion == idVersion);
        if (version == null) return NotFound();

        var doc = await _db.Documentos.FirstOrDefaultAsync(d => d.IdDocumento == version.IdDocumento);
        if (doc == null) return NotFound();
        if (doc.IdEmpresa != EmpresaActual()) return Forbid();

        var idPendienteRev = await EstadoIdAsync("Pendiente de revision");
        if (version.IdEstado != idPendienteRev)
            return RedirectToAction("Detalle", new { id = version.IdDocumento });

        if (string.IsNullOrWhiteSpace(comentario))
        {
            TempData["Error"] = "Debes escribir el motivo del rechazo.";
            return RedirectToAction("Detalle", new { id = version.IdDocumento });
        }

        version.IdEstado = await EstadoIdAsync("Rechazado");
        version.IdUsuarioRevisor = UsuarioActual();
        version.FechaRevision = DateTime.UtcNow;

        _db.Revisiones.Add(new Revision
        {
            IdVersion = version.IdVersion,
            IdUsuario = UsuarioActual(),
            Tipo = "RECHAZO_REVISION",
            Comentario = comentario.Trim(),
            FechaHora = DateTime.UtcNow
        });
        _db.Notificaciones.Add(new Notificacion
        {
            IdUsuario = version.IdUsuarioSubio,
            Mensaje = $"Tu documento \"{doc.Titulo}\" (v{version.VersionMajor}.{version.VersionMinor}) fue RECHAZADO en la revision: {comentario.Trim()}",
            IdVersion = version.IdVersion,
            Leida = false,
            FechaCreacion = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        await _bitacora.RegistrarAsync("RECHAZAR", "VERSION", version.IdVersion, doc.Titulo);
        return RedirectToAction("Detalle", new { id = version.IdDocumento });
    }

    // ---------- Aprobar (Aprobador, paso final) ----------
    [HttpPost]
    [Authorize(Roles = "Aprobador")]
    public async Task<IActionResult> Aprobar(int idVersion)
    {
        var version = await _db.Versiones.FirstOrDefaultAsync(v => v.IdVersion == idVersion);
        if (version == null) return NotFound();

        var doc = await _db.Documentos.FirstOrDefaultAsync(d => d.IdDocumento == version.IdDocumento);
        if (doc == null) return NotFound();
        if (doc.IdEmpresa != EmpresaActual()) return Forbid();

        var idPendiente = await EstadoIdAsync("Pendiente de aprobacion");
        if (version.IdEstado != idPendiente)
            return RedirectToAction("Detalle", new { id = version.IdDocumento });

        var idAprobado = await EstadoIdAsync("Aprobado");
        var idObsoleto = await EstadoIdAsync("Obsoleto");

        // La version aprobada anterior (si existe) pasa a Obsoleto
        await _db.Versiones
            .Where(v => v.IdDocumento == doc.IdDocumento && v.IdEstado == idAprobado && v.IdVersion != version.IdVersion)
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.IdEstado, idObsoleto));

        // Numeracion: el documento arranca en 0.1 y sube de decimal con cada
        // correccion (0.1 -> 0.2 ...). Al APROBARSE pasa a la siguiente entera
        // (0.1 -> 1.0, 1.2 -> 2.0). Asi un aprobado siempre es un numero entero.
        if (version.VersionMinor > 0)
        {
            version.VersionMajor += 1;
            version.VersionMinor = 0;
        }

        version.IdEstado = idAprobado;
        version.IdUsuarioRevisor = UsuarioActual();
        version.FechaRevision = DateTime.UtcNow;

        _db.Revisiones.Add(new Revision
        {
            IdVersion = version.IdVersion,
            IdUsuario = UsuarioActual(),
            Tipo = "APROBACION",
            Comentario = null,
            FechaHora = DateTime.UtcNow
        });

        _db.Notificaciones.Add(new Notificacion
        {
            IdUsuario = version.IdUsuarioSubio,
            Mensaje = $"Tu documento \"{doc.Titulo}\" (v{version.VersionMajor}.{version.VersionMinor}) fue APROBADO.",
            IdVersion = version.IdVersion,
            Leida = false,
            FechaCreacion = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        await _bitacora.RegistrarAsync("APROBAR", "VERSION", version.IdVersion, doc.Titulo);

        // ----- Integracion con el modulo de Indexacion (Node + MongoDB) -----
        // Al aprobar, enviamos el metadato del documento para que quede indexado
        // y aparezca en la busqueda. Si Node esta caido, no se rompe la aprobacion.
        var nombreEmpresa = await _db.Empresas
            .Where(e => e.IdEmpresa == doc.IdEmpresa).Select(e => e.Nombre).FirstOrDefaultAsync() ?? "";
        var nombreCategoria = await _db.Categorias
            .Where(c => c.IdCategoria == doc.IdCategoria).Select(c => c.Nombre).FirstOrDefaultAsync() ?? "";
        var subioPor = await _db.Usuarios
            .Where(u => u.IdUsuario == version.IdUsuarioSubio).Select(u => u.NombreCompleto).FirstOrDefaultAsync() ?? "";

        // Extraemos el texto del archivo (PDF, Word .docx o .txt) para indexarlo
        // (busqueda por contenido y descarga .txt). Si no se puede, queda vacio.
        var textoExtraido = ExtraerTexto(version.Archivo, version.Extension);

        var metadato = new
        {
            idDocumentoCentral = doc.IdDocumento,
            idVersionCentral = version.IdVersion,
            idEmpresa = doc.IdEmpresa,
            nombreEmpresa,
            titulo = doc.Titulo,
            categoria = nombreCategoria,
            numeroVersion = version.VersionMajor,
            estado = "Aprobado",
            etiquetas = new[] { nombreCategoria.ToLowerInvariant() },
            nombreArchivo = version.NombreArchivo,
            extension = version.Extension.TrimStart('.'),
            subidoPor = subioPor,
            fechaSubida = version.FechaSubida,
            fechaAprobacion = version.FechaRevision ?? DateTime.UtcNow,
            textoCompleto = textoExtraido
        };

        bool indexado = await _indexacion.IndexarAsync(metadato);
        await _bitacora.RegistrarAsync(
            indexado ? "INDEXADO" : "INDEXADO_ERROR", "VERSION", version.IdVersion,
            indexado ? "Metadato enviado al modulo de indexacion" : "No se pudo contactar al modulo de indexacion");

        // ----- Integracion con el modulo de Consulta (PHP + PostgreSQL) -----
        // Publicamos el documento aprobado para que aparezca en el portal y en los reportes.
        var aprobadoPor = User.Identity?.Name ?? "";
        var docConsulta = new
        {
            idDocumentoCentral = doc.IdDocumento,
            idVersionCentral = version.IdVersion,
            idEmpresa = doc.IdEmpresa,
            nombreEmpresa,
            titulo = doc.Titulo,
            categoria = nombreCategoria,
            numeroVersion = version.VersionMajor,
            nombreArchivo = version.NombreArchivo,
            extension = version.Extension.TrimStart('.'),
            tamanoBytes = version.TamanoBytes,
            aprobadoPor,
            fechaAprobacion = version.FechaRevision ?? DateTime.UtcNow
        };
        bool publicado = await _consulta.PublicarAprobadoAsync(docConsulta);
        await _bitacora.RegistrarAsync(
            publicado ? "PUBLICADO" : "PUBLICADO_ERROR", "VERSION", version.IdVersion,
            publicado ? "Documento publicado en el portal de consulta" : "No se pudo contactar al portal de consulta");

        return RedirectToAction("Detalle", new { id = version.IdDocumento });
    }

    // ---------- Rechazar (Aprobador, paso final) ----------
    [HttpPost]
    [Authorize(Roles = "Aprobador")]
    public async Task<IActionResult> Rechazar(int idVersion, string? comentario)
    {
        var version = await _db.Versiones.FirstOrDefaultAsync(v => v.IdVersion == idVersion);
        if (version == null) return NotFound();

        var doc = await _db.Documentos.FirstOrDefaultAsync(d => d.IdDocumento == version.IdDocumento);
        if (doc == null) return NotFound();
        if (doc.IdEmpresa != EmpresaActual()) return Forbid();

        var idPendiente = await EstadoIdAsync("Pendiente de aprobacion");
        if (version.IdEstado != idPendiente)
            return RedirectToAction("Detalle", new { id = version.IdDocumento });

        if (string.IsNullOrWhiteSpace(comentario))
        {
            TempData["Error"] = "Debes escribir el motivo del rechazo.";
            return RedirectToAction("Detalle", new { id = version.IdDocumento });
        }

        version.IdEstado = await EstadoIdAsync("Rechazado");
        version.IdUsuarioRevisor = UsuarioActual();
        version.FechaRevision = DateTime.UtcNow;

        _db.Revisiones.Add(new Revision
        {
            IdVersion = version.IdVersion,
            IdUsuario = UsuarioActual(),
            Tipo = "RECHAZO",
            Comentario = comentario.Trim(),
            FechaHora = DateTime.UtcNow
        });

        _db.Notificaciones.Add(new Notificacion
        {
            IdUsuario = version.IdUsuarioSubio,
            Mensaje = $"Tu documento \"{doc.Titulo}\" (v{version.VersionMajor}.{version.VersionMinor}) fue RECHAZADO: {comentario.Trim()}",
            IdVersion = version.IdVersion,
            Leida = false,
            FechaCreacion = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        await _bitacora.RegistrarAsync("RECHAZAR", "VERSION", version.IdVersion, doc.Titulo);
        return RedirectToAction("Detalle", new { id = version.IdDocumento });
    }

    // ---------- Subir nueva version (cuando la ultima fue rechazada) ----------
    [HttpPost]
    [Authorize(Roles = "Supervisor")]
    public async Task<IActionResult> NuevaVersion(int idDocumento, IFormFile? archivo)
    {
        var doc = await _db.Documentos.FirstOrDefaultAsync(d => d.IdDocumento == idDocumento && d.Activo);
        if (doc == null) return NotFound();
        if (doc.IdEmpresa != EmpresaActual()) return Forbid();

        var ultima = await _db.Versiones
            .Where(v => v.IdDocumento == idDocumento)
            .OrderByDescending(v => v.NumeroVersion)
            .Select(v => new { v.NumeroVersion, v.IdEstado, v.VersionMajor, v.VersionMinor })
            .FirstOrDefaultAsync();

        var idRechazado = await EstadoIdAsync("Rechazado");
        if (ultima == null || ultima.IdEstado != idRechazado)
        {
            TempData["Error"] = "Solo puedes subir una nueva version cuando la ultima fue rechazada.";
            return RedirectToAction("Detalle", new { id = idDocumento });
        }

        if (archivo == null || archivo.Length == 0)
        {
            TempData["Error"] = "Selecciona un archivo.";
            return RedirectToAction("Detalle", new { id = idDocumento });
        }

        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!ExtensionesPermitidas.Contains(extension))
        {
            TempData["Error"] = "Tipo de archivo no permitido.";
            return RedirectToAction("Detalle", new { id = idDocumento });
        }

        var idPendiente = await EstadoIdAsync("Pendiente de revision");

        using var ms = new MemoryStream();
        await archivo.CopyToAsync(ms);
        var bytes = ms.ToArray();

        var nuevaVersion = new DocumentoVersion
        {
            IdDocumento = idDocumento,
            NumeroVersion = ultima.NumeroVersion + 1,
            VersionMajor = ultima.VersionMajor,
            VersionMinor = ultima.VersionMinor + 1,
            IdEstado = idPendiente,
            IdUsuarioSubio = UsuarioActual(),
            NombreArchivo = Path.GetFileName(archivo.FileName),
            Extension = extension,
            TamanoBytes = bytes.LongLength,
            Archivo = bytes,
            FechaSubida = DateTime.UtcNow,
            Activo = true
        };
        _db.Versiones.Add(nuevaVersion);
        await _db.SaveChangesAsync();

        // La correccion vuelve a empezar el flujo: avisar a los Revisores.
        var revisoresNV = await IdsUsuariosPorRolAsync("Revisor", doc.IdEmpresa);
        NotificarUsuarios(revisoresNV,
            $"Hay una version corregida por revisar: \"{doc.Titulo}\" (v{nuevaVersion.VersionMajor}.{nuevaVersion.VersionMinor}).",
            nuevaVersion.IdVersion);
        await _db.SaveChangesAsync();

        await _bitacora.RegistrarAsync("NUEVA_VERSION", "DOCUMENTO", idDocumento, doc.Titulo);
        return RedirectToAction("Detalle", new { id = idDocumento });
    }

    // ---------- Reportar error en un documento aprobado (lo reabre) ----------
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ReportarError(int idVersion, string? comentario)
    {
        var info = await _db.Versiones
            .Where(v => v.IdVersion == idVersion)
            .Select(v => new { v.IdDocumento, v.IdEstado, v.IdUsuarioSubio, v.NumeroVersion })
            .FirstOrDefaultAsync();
        if (info == null) return NotFound();

        var doc = await _db.Documentos.FirstOrDefaultAsync(d => d.IdDocumento == info.IdDocumento);
        if (doc == null) return NotFound();
        if (doc.IdEmpresa != EmpresaActual()) return Forbid();

        var idAprobado = await EstadoIdAsync("Aprobado");
        if (info.IdEstado != idAprobado)
            return RedirectToAction("Detalle", new { id = info.IdDocumento });

        if (string.IsNullOrWhiteSpace(comentario))
        {
            TempData["Error"] = "Describe el error que encontraste.";
            return RedirectToAction("Detalle", new { id = info.IdDocumento });
        }

        // Reabrir: la version vigente vuelve a Rechazado
        var idRechazado = await EstadoIdAsync("Rechazado");
        await _db.Versiones
            .Where(v => v.IdVersion == idVersion)
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.IdEstado, idRechazado));

        _db.Revisiones.Add(new Revision
        {
            IdVersion = idVersion,
            IdUsuario = UsuarioActual(),
            Tipo = "REPORTE_ERROR",
            Comentario = comentario.Trim(),
            FechaHora = DateTime.UtcNow
        });
        _db.Notificaciones.Add(new Notificacion
        {
            IdUsuario = info.IdUsuarioSubio,
            Mensaje = $"Se reporto un error en tu documento aprobado \"{doc.Titulo}\" (v{info.NumeroVersion}). Sube una version corregida.",
            IdVersion = idVersion,
            Leida = false,
            FechaCreacion = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        await _bitacora.RegistrarAsync("REPORTE_ERROR", "VERSION", idVersion, doc.Titulo);
        return RedirectToAction("Detalle", new { id = info.IdDocumento });
    }

    // ---------- Prerrevision: comentario de roles inferiores mientras esta Pendiente ----------
    [HttpPost]
    [Authorize(Roles = "Empleado,Supervisor")]
    public async Task<IActionResult> Prerrevisar(int idVersion, string? comentario)
    {
        var info = await _db.Versiones
            .Where(v => v.IdVersion == idVersion)
            .Select(v => new { v.IdDocumento, v.IdEstado })
            .FirstOrDefaultAsync();
        if (info == null) return NotFound();

        var doc = await _db.Documentos.FirstOrDefaultAsync(d => d.IdDocumento == info.IdDocumento);
        if (doc == null) return NotFound();
        if (doc.IdEmpresa != EmpresaActual()) return Forbid();

        var idPendiente = await EstadoIdAsync("Pendiente de revision");
        if (info.IdEstado != idPendiente)
            return RedirectToAction("Detalle", new { id = info.IdDocumento });

        if (string.IsNullOrWhiteSpace(comentario))
        {
            TempData["Error"] = "Escribe tu comentario de prerrevision.";
            return RedirectToAction("Detalle", new { id = info.IdDocumento });
        }

        _db.Revisiones.Add(new Revision
        {
            IdVersion = idVersion,
            IdUsuario = UsuarioActual(),
            Tipo = "PREREVISION",
            Comentario = comentario.Trim(),
            FechaHora = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return RedirectToAction("Detalle", new { id = info.IdDocumento });
    }

    // ---------- Descargar archivo de una version ----------
    public async Task<IActionResult> Descargar(int idVersion)
    {
        var v = await _db.Versiones
            .Include(x => x.Documento)
            .FirstOrDefaultAsync(x => x.IdVersion == idVersion);

        if (v == null || v.Documento == null) return NotFound();

        if (!EsAdmin && v.Documento.IdEmpresa != EmpresaActual())
            return Forbid();

        await _bitacora.RegistrarAsync("DESCARGAR", "VERSION", v.IdVersion, v.NombreArchivo);
        return File(v.Archivo, "application/octet-stream", v.NombreArchivo);
    }

    // ---------- Ver archivo en el navegador (inline, sin descargar) ----------
    public async Task<IActionResult> Ver(int idVersion)
    {
        var v = await _db.Versiones
            .Include(x => x.Documento)
            .FirstOrDefaultAsync(x => x.IdVersion == idVersion);

        if (v == null || v.Documento == null) return NotFound();

        if (!EsAdmin && v.Documento.IdEmpresa != EmpresaActual())
            return Forbid();

        await _bitacora.RegistrarAsync("VER", "VERSION", v.IdVersion, v.NombreArchivo);
        // File(bytes, contentType) SIN nombre => el navegador lo muestra inline
        return File(v.Archivo, ContentTypePorExtension(v.Extension));
    }

    // Tipo MIME segun la extension (para que el navegador sepa como mostrarlo).
    public static string ContentTypePorExtension(string extension)
    {
        return extension.ToLowerInvariant().TrimStart('.') switch
        {
            "pdf" => "application/pdf",
            "png" => "image/png",
            "jpg" or "jpeg" => "image/jpeg",
            "gif" => "image/gif",
            "txt" => "text/plain; charset=utf-8",
            _ => "application/octet-stream",
        };
    }

    // Indica si el navegador puede mostrar ese tipo directamente (PDF, imagen, texto).
    public static bool SePuedeVer(string extension) =>
        new[] { "pdf", "png", "jpg", "jpeg", "gif", "txt" }
            .Contains(extension.ToLowerInvariant().TrimStart('.'));

    // Extrae el texto de un archivo segun su tipo. Si algo falla, devuelve "".
    private static string ExtraerTexto(byte[] archivo, string extension)
    {
        var ext = extension.ToLowerInvariant().TrimStart('.');
        try
        {
            return ext switch
            {
                "pdf"  => ExtraerTextoPdf(archivo),
                "docx" => ExtraerTextoDocx(archivo),
                "txt"  => System.Text.Encoding.UTF8.GetString(archivo).Trim(),
                _      => "",
            };
        }
        catch
        {
            return "";
        }
    }

    // PDF: usa la libreria PdfPig.
    private static string ExtraerTextoPdf(byte[] archivo)
    {
        var sb = new System.Text.StringBuilder();
        using var doc = UglyToad.PdfPig.PdfDocument.Open(archivo);
        foreach (var page in doc.GetPages())
            sb.AppendLine(page.Text);
        return sb.ToString().Trim();
    }

    // Word (.docx): un .docx es un ZIP con XML dentro. Leemos word/document.xml
    // y juntamos el texto de los parrafos (sin librerias extra).
    private static string ExtraerTextoDocx(byte[] archivo)
    {
        using var ms = new System.IO.MemoryStream(archivo);
        using var zip = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Read);
        var entry = zip.GetEntry("word/document.xml");
        if (entry == null) return "";

        using var stream = entry.Open();
        var doc = System.Xml.Linq.XDocument.Load(stream);
        System.Xml.Linq.XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

        var sb = new System.Text.StringBuilder();
        foreach (var parrafo in doc.Descendants(w + "p"))
        {
            foreach (var texto in parrafo.Descendants(w + "t"))
                sb.Append(texto.Value);
            sb.AppendLine();
        }
        return sb.ToString().Trim();
    }
}
