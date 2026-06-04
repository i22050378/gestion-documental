using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Central.Models;

[Table("Estados")]
public class Estado
{
    [Key] public int IdEstado { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
}
