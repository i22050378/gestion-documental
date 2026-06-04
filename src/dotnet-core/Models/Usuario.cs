using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Central.Models;

[Table("Usuarios")]
public class Usuario
{
    [Key]
    public int IdUsuario { get; set; }
    public int? IdEmpresa { get; set; }   // NULL = Admin global
    public int IdRol { get; set; }
    public string NombreCompleto { get; set; } = "";
    public string Correo { get; set; } = "";
    public string ContrasenaHash { get; set; } = "";
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? UltimoAcceso { get; set; }

    // Relaciones (la columna que las une se indica con ForeignKey)
    [ForeignKey(nameof(IdRol))]
    public Rol? Rol { get; set; }

    [ForeignKey(nameof(IdEmpresa))]
    public Empresa? Empresa { get; set; }
}
