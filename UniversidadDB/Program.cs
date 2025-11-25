using Microsoft.EntityFrameworkCore;
using UniversidadDB.Data;
using UniversidadDB.Services;

var builder = WebApplication.CreateBuilder(args);

// ✅ DbContext con SQL Server (Azure SQL)
builder.Services.AddDbContext<UniversidadContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ Servicios
builder.Services.AddScoped<EmailService>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<FcmService>();

// ✅ Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ✅ Swagger SIEMPRE (incluye Render/Production)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "UniversidadDB API v1");
    c.RoutePrefix = "swagger"; // => /swagger
    // Si quieres Swagger en la raíz "/", usa: c.RoutePrefix = "";
});

// Si luego habilitas auth con JWT, normalmente va antes de Authorization:
// app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ✅ Render: escuchar en el puerto que Render asigna (por defecto 10000)
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();
