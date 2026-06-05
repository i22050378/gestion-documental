using Central.Data;
using Central.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Central.Services.BitacoraService>();

builder.Services.AddDbContext<CentralDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CentralDB")!));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

var app = builder.Build();

await SeedPasswordsAsync(app);

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Contrasena por defecto para los usuarios de prueba: Demo123!
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
