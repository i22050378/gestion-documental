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

    public bool PuedeAprobar { get; set; }     // Director, misma empresa, ultima = Pendiente
    public bool PuedeSubirNueva { get; set; }  // Supervisor/Director, misma empresa, ultima = Rechazado
}

public class VersionRow
{
    public int IdVersion { get; set; }
    public int NumeroVersion { get; set; }
    public string Estado { get; set; } = "";
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
