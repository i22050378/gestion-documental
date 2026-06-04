using Central.Models;
using Microsoft.EntityFrameworkCore;

namespace Central.Data;

// Contexto de EF Core: representa la base CentralDB y sus tablas.
// Por ahora mapeamos solo las tablas de identidad; las de documentos
// las agregaremos cuando construyamos esa parte.
public class CentralDbContext : DbContext
{
    public CentralDbContext(DbContextOptions<CentralDbContext> options) : base(options) { }

    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
}
