using Microsoft.EntityFrameworkCore;
using UniversidadDB.Data;

var builder = WebApplication.CreateBuilder(args);

// DbContext con SQL Server
builder.Services.AddDbContext<UniversidadContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Registrar EmailService en el contenedor de dependencias
builder.Services.AddScoped<EmailService>(); // Esto hace que el servicio EmailService esté disponible

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configurar el puerto dinámico
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";  // Usar 5000 como fallback si no se establece
app.Urls.Add($"http://*:{port}");  // Configurar la aplicación para que escuche en el puerto correcto

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // Puedes activar esto si deseas usar HTTPS

app.UseAuthorization();

app.MapControllers();

app.Run();
