using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Central.Models;

[Table("Documentos")]
public class Documento
{
    [Key] public int IdDocumento { get; set; }
    public int IdEmpresa { get; set; }
    public int IdCategoria { get; set; }
    public int IdUsuarioCreador { get; set; }
    public string Titulo { get; set; } = "";
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }

    [ForeignKey(nameof(IdCategoria))]
    public Categoria? Categoria { get; set; }

    [ForeignKey(nameof(IdUsuarioCreador))]
    public Usuario? Creador { get; set; }

    [ForeignKey(nameof(IdEmpresa))]
    public Empresa? Empresa { get; set; }

    public List<DocumentoVersion> Versiones { get; set; } = new();
}
