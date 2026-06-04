using Central.Models;
using Microsoft.EntityFrameworkCore;

namespace Central.Data;

public class CentralDbContext : DbContext
{
    public CentralDbContext(DbContextOptions<CentralDbContext> options) : base(options) { }

    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
}
