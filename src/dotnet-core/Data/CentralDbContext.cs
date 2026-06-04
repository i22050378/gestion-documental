using Central.Models;
using Microsoft.EntityFrameworkCore;

namespace Central.Data;

public class CentralDbContext : DbContext
{
    public CentralDbContext(DbContextOptions<CentralDbContext> options) : base(options) { }

    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Estado> Estados => Set<Estado>();
    public DbSet<Documento> Documentos => Set<Documento>();
    public DbSet<DocumentoVersion> Versiones => Set<DocumentoVersion>();
    public DbSet<Revision> Revisiones => Set<Revision>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();
}
