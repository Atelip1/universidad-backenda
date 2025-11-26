using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversidadDB.Data;
using UniversidadDB.Data;
using UniversidadDB.Models.Comunidad;
[ApiController]
[Route("api/[controller]")]
public class ComunidadController : ControllerBase
{
    private readonly UniversidadContext _context;
    public ComunidadController(UniversidadContext context) => _context = context;

    // ✅ Feed general
    [HttpGet("posts")]
    public IActionResult GetPosts()
    {
        var posts = _context.Posts
            .Where(p => !p.Oculto)
            .OrderByDescending(p => p.FechaCreacion)
            .Select(p => new {
                p.Id,
                p.Contenido,
                p.FechaCreacion,
                p.EtiquetaCurso,
                Likes = p.Likes.Count,
                Comentarios = p.Comentarios.Count
            }).ToList();
        return Ok(posts);
    }

    // ✅ Crear post
    [HttpPost("posts")]
    public async Task<IActionResult> CreatePost([FromBody] Post post)
    {
        post.FechaCreacion = DateTime.UtcNow;
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();
        return Ok(post);
    }

    // ❤️ Like/unlike
    [HttpPost("posts/{id}/like")]
    public async Task<IActionResult> ToggleLike(int id, [FromQuery] int userId)
    {
        var like = _context.Likes.FirstOrDefault(l => l.PostId == id && l.UsuarioId == userId);
        if (like == null)
        {
            _context.Likes.Add(new Like { PostId = id, UsuarioId = userId });
        }
        else
        {
            _context.Likes.Remove(like);
        }
        await _context.SaveChangesAsync();
        return Ok();
    }

    // 💬 Comentar
    [HttpPost("posts/{id}/comentarios")]
    public async Task<IActionResult> AddComment(int id, [FromBody] Comentario c)
    {
        c.PostId = id;
        c.FechaCreacion = DateTime.UtcNow;
        _context.Comentarios.Add(c);
        await _context.SaveChangesAsync();
        return Ok(c);
    }

    // 🚨 Reportar
    [HttpPost("reportar")]
    public async Task<IActionResult> Reportar([FromBody] Reporte reporte)
    {
        // Obtener el UsuarioId del token (suponiendo que el UserId está en el claim "sub" o "NameIdentifier")
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId == null)
        {
            return Unauthorized(new { message = "Usuario no autenticado." });
        }

        // Asignar el ReportadoPorId con el UsuarioId del usuario autenticado
        reporte.ReportadoPorId = int.Parse(userId);  // Convertir el usuarioId a int si es necesario

        // Guardar el reporte
        _context.Reportes.Add(reporte);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Reporte enviado." });
    }


    // 🧹 Admin: listar reportes
    [HttpGet("reportes")]
    public IActionResult GetReportes()
    {
        var r = _context.Reportes.OrderByDescending(x => x.Fecha).ToList();
        return Ok(r);
    }

    // 🧩 Admin: ocultar post
    [HttpPut("posts/{id}/ocultar")]
    public async Task<IActionResult> OcultarPost(int id)
    {
        var post = await _context.Posts.FindAsync(id);
        if (post == null) return NotFound();
        post.Oculto = true;
        await _context.SaveChangesAsync();
        return Ok();
    }
}
