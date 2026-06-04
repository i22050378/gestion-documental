using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Central.Models;

[Table("Categorias")]
public class Categoria
{
    [Key] public int IdCategoria { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
}
