using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Central.Models;

[Table("Bitacora")]
public class Bitacora
{
    [Key] public int IdBitacora { get; set; }
    public int? IdUsuario { get; set; }      // NULL en intentos de login fallidos
    public string Accion { get; set; } = "";
    public string? Entidad { get; set; }
    public int? IdEntidad { get; set; }
    public string? Detalle { get; set; }
    public string? DireccionIP { get; set; }
    public DateTime FechaHora { get; set; }
}
