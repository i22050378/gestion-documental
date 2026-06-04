using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Central.Models;

public class DocumentoFormViewModel
{
    [Required(ErrorMessage = "Ingresa un titulo")]
    public string Titulo { get; set; } = "";

    public string? Descripcion { get; set; }

    [Required(ErrorMessage = "Elige una categoria")]
    public int IdCategoria { get; set; }

    public IFormFile? Archivo { get; set; }

    public string? Error { get; set; }

    public List<Categoria> Categorias { get; set; } = new();
}
