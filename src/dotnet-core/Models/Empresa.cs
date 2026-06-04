using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Central.Models;

[Table("Empresas")]
public class Empresa
{
    [Key] public int IdEmpresa { get; set; }
    public string Nombre { get; set; } = "";
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
}
