using Central.Data;
using Central.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Conexion a SQL Server (la cadena viene de appsettings.json)
builder.Services.AddDbContext<CentralDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CentralDB")!));

// Autenticacion por cookie: si no hay sesion, manda al login
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

var app = builder.Build();

// Al iniciar, ponerle contrasena real (cifrada) a los usuarios semilla
// que aun tengan el marcador "PENDIENTE_HASH".
await SeedPasswordsAsync(app);

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Contrasena por defecto para los 4 usuarios de prueba: Demo123!
static async Task SeedPasswordsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CentralDbContext>();
    var hasher = new PasswordHasher<Usuario>();

    var pendientes = await db.Usuarios
        .Where(u => u.ContrasenaHash == "PENDIENTE_HASH")
        .ToListAsync();

    foreach (var u in pendientes)
        u.ContrasenaHash = hasher.HashPassword(u, "Demo123!");

    if (pendientes.Count > 0)
        await db.SaveChangesAsync();
}
