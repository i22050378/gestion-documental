using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Central.Models;

[Table("Notificaciones")]
public class Notificacion
{
    [Key] public int IdNotificacion { get; set; }
    public int IdUsuario { get; set; }
    public string Mensaje { get; set; } = "";
    public int? IdVersion { get; set; }
    public bool Leida { get; set; }
    public DateTime FechaCreacion { get; set; }
}
