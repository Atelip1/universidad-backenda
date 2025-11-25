using System;
using Microsoft.EntityFrameworkCore;
using UniversidadDB.Data;
using UniversidadDB.Models;
using UniversidadDB.Services;
using UniversidadDB.Services;
var builder = WebApplication.CreateBuilder(args);

// DbContext con SQL Server
builder.Services.AddDbContext<UniversidadContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Registrar EmailService en el contenedor de dependencias

builder.Services.AddScoped<EmailService>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<FcmService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// 👇 Para Render, si aún no lo pusiste:
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();
