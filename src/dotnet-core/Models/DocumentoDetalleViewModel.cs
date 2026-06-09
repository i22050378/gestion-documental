namespace Central.Models;

public class DocumentoDetalleViewModel
{
    public int IdDocumento { get; set; }
    public string Titulo { get; set; } = "";
    public string? Descripcion { get; set; }
    public string Categoria { get; set; } = "";
    public string Empresa { get; set; } = "";
    public string Creador { get; set; } = "";
    public DateTime FechaCreacion { get; set; }

    public List<VersionRow> Versiones { get; set; } = new();
    public List<RevisionRow> Revisiones { get; set; } = new();

    public int IdUltimaVersion { get; set; }
    public string EstadoUltima { get; set; } = "";

    public bool PuedeAprobar { get; set; }       // Director, misma empresa, ultima = Pendiente
    public bool PuedeSubirNueva { get; set; }    // Supervisor/Director, misma empresa, ultima = Rechazado
    public bool PuedeReportarError { get; set; } // misma empresa, ultima = Aprobado
    public bool PuedePrerrevisar { get; set; }   // Empleado/Supervisor, misma empresa, ultima = Pendiente
}

public class VersionRow
{
    public int IdVersion { get; set; }
    public int NumeroVersion { get; set; }
    public int VersionMajor { get; set; }
    public int VersionMinor { get; set; }
    public string Estado { get; set; } = "";
    public string Extension { get; set; } = "";
    public bool SePuedeVer =>
        new[] { "pdf", "png", "jpg", "jpeg", "gif", "txt" }
            .Contains((Extension ?? "").ToLowerInvariant().TrimStart('.'));
    public string SubioPor { get; set; } = "";
    public DateTime FechaSubida { get; set; }
    public long TamanoBytes { get; set; }
    public bool EsVigente { get; set; }   // estado == Aprobado
}

public class RevisionRow
{
    public string Tipo { get; set; } = "";
    public string? Comentario { get; set; }
    public string Usuario { get; set; } = "";
    public DateTime FechaHora { get; set; }
}
