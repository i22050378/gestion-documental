using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Central.Models;

[Table("Versiones")]
public class DocumentoVersion
{
    [Key] public int IdVersion { get; set; }
    public int IdDocumento { get; set; }
    public int NumeroVersion { get; set; }     // contador secuencial interno (1,2,3...)
    public int VersionMajor { get; set; }      // parte entera para mostrar (1.x, 2.x)
    public int VersionMinor { get; set; }      // parte decimal (sube en cada correccion)
    public int IdEstado { get; set; }
    public int IdUsuarioSubio { get; set; }
    public string NombreArchivo { get; set; } = "";
    public string Extension { get; set; } = "";
    public long TamanoBytes { get; set; }
    public byte[] Archivo { get; set; } = Array.Empty<byte>();
    public DateTime FechaSubida { get; set; }
    public int? IdUsuarioRevisor { get; set; }
    public DateTime? FechaRevision { get; set; }
    public bool Activo { get; set; }

    [ForeignKey(nameof(IdDocumento))]
    public Documento? Documento { get; set; }

    [ForeignKey(nameof(IdEstado))]
    public Estado? Estado { get; set; }
}
