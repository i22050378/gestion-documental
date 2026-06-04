namespace Central.Models;

// Modelo ligero para la lista (no carga el archivo binario)
public class DocumentoListItem
{
    public int IdDocumento { get; set; }
    public string Titulo { get; set; } = "";
    public string Categoria { get; set; } = "";
    public string Creador { get; set; } = "";
    public string Empresa { get; set; } = "";
    public DateTime FechaCreacion { get; set; }
    public int UltimaVersion { get; set; }
    public int IdEstadoUltima { get; set; }
    public string? Estado { get; set; }
    public int IdUltimaVersion { get; set; }
}
