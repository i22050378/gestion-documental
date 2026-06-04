using System.ComponentModel.DataAnnotations;

namespace Central.Models;

public class EmpresaFormViewModel
{
    [Required(ErrorMessage = "Ingresa el nombre de la empresa")]
    public string Nombre { get; set; } = "";

    public string? Error { get; set; }
}
