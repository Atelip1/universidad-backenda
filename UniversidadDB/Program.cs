using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using UniversidadDB.Data;
using UniversidadDB.Services;

var builder = WebApplication.CreateBuilder(args);

// ✅ Render: bind al puerto asignado
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// ✅ DbContext
builder.Services.AddDbContext<UniversidadContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ Servicios
builder.Services.AddScoped<EmailService>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<FcmService>();
builder.Services.AddSingleton<JwtService>();

builder.Services.AddControllers();

// ✅ JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "CAMBIA_ESTA_CLAVE_SUPER_SECRETA";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ✅ Swagger + botón Authorize
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "UniversidadDB API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Escribe: Bearer {tu_token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ✅ Swagger en Render/Producción también
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "UniversidadDB API v1");
    c.RoutePrefix = "swagger";
});

// ✅ Crear carpeta Uploads si no existe (EVITA: DirectoryNotFoundException)
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "Uploads");
Directory.CreateDirectory(uploadsPath);

// ✅ Servir archivos subidos: https://tuapi/Uploads/archivo.pdf
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/Uploads"
});

// ✅ Importante: Authentication ANTES que Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
