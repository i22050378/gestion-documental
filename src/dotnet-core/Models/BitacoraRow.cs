namespace Central.Models;

public class BitacoraRow
{
    public DateTime FechaHora { get; set; }
    public int? IdUsuario { get; set; }
    public string Usuario { get; set; } = "";
    public string Accion { get; set; } = "";
    public string? Entidad { get; set; }
    public int? IdEntidad { get; set; }
    public string? Detalle { get; set; }
    public string? DireccionIP { get; set; }
}
