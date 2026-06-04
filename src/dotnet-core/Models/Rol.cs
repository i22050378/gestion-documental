using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Central.Models;

[Table("Roles")]
public class Rol
{
    [Key]
    public int IdRol { get; set; }
    public string Nombre { get; set; } = "";
    public int Nivel { get; set; }
    public string? Descripcion { get; set; }
}
