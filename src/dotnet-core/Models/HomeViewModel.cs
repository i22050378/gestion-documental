namespace Central.Models;

public class HomeViewModel
{
    public bool Conectado { get; set; }
    public string? Error { get; set; }
    public List<Rol> Roles { get; set; } = new();
    public List<Usuario> Usuarios { get; set; } = new();
}
