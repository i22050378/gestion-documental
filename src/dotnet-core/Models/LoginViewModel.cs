using System.ComponentModel.DataAnnotations;

namespace Central.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Ingresa tu correo")]
    [EmailAddress(ErrorMessage = "Correo no valido")]
    public string Correo { get; set; } = "";

    [Required(ErrorMessage = "Ingresa tu contrasena")]
    public string Contrasena { get; set; } = "";

    public string? Error { get; set; }
}
