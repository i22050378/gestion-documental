using System.ComponentModel.DataAnnotations;

namespace Central.Models;

public class UsuarioFormViewModel
{
    public int IdUsuario { get; set; }

    [Required(ErrorMessage = "Ingresa el nombre")]
    public string NombreCompleto { get; set; } = "";

    [Required(ErrorMessage = "Ingresa el correo")]
    [EmailAddress(ErrorMessage = "Correo no valido")]
    public string Correo { get; set; } = "";

    public string? Contrasena { get; set; }

    [Required(ErrorMessage = "Elige un rol")]
    public int IdRol { get; set; }

    public int? IdEmpresa { get; set; }

    public bool Activo { get; set; } = true;

    public string? Error { get; set; }

    public List<Rol> Roles { get; set; } = new();
    public List<Empresa> Empresas { get; set; } = new();
}
