using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Central.Models;

[Table("Revisiones")]
public class Revision
{
    [Key] public int IdRevision { get; set; }
    public int IdVersion { get; set; }
    public int IdUsuario { get; set; }
    public string Tipo { get; set; } = "";   // APROBACION, RECHAZO, REPORTE_ERROR, PREREVISION, COMENTARIO
    public string? Comentario { get; set; }
    public DateTime FechaHora { get; set; }

    [ForeignKey(nameof(IdUsuario))]
    public Usuario? Usuario { get; set; }
}
