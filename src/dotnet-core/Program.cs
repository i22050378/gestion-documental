using Central.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC (controladores + vistas)
builder.Services.AddControllersWithViews();

// Conexion a SQL Server mediante EF Core.
// La cadena viene de appsettings.json (o de una variable de entorno en Docker).
builder.Services.AddDbContext<CentralDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CentralDB")!));

var app = builder.Build();

app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
