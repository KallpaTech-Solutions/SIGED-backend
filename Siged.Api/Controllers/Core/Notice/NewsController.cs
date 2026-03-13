using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siged.Application.DTOs.Core.Nocice;
using Siged.Domain.Entities.Core.Notice;
using Siged.Domain.Entities.Security;
using Siged.Infrastructure.Persistence;
using System.Net.Mime;

namespace Siged.Api.Controllers.Core.Notice
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    public class NewsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene el feed público de noticias (Solo publicadas).
        /// </summary>
        [HttpGet("feed")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicFeed()
        {
            var newsList = await _context.News.Include(n => n.Media)
                .Where(n => n.Status == NewsStatus.Published)
                .OrderByDescending(n => n.IsFeatured).ThenByDescending(n => n.CreatedAt)
                .ToListAsync();

            return Ok(newsList.Select(MapToResponseDto));
        }

        /// <summary>
        /// Lista de noticias para administración (Incluye todos los estados).
        /// </summary>
        [HttpGet("admin")]
        [Authorize(Policy = Permissions.NewsView)]
        public async Task<IActionResult> GetAllAdmin()
        {
            var newsList = await _context.News
                .Include(n => n.Media)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            var response = newsList.Select(MapToResponseDto);
            return Ok(response);
        }

        /// <summary>
        /// Crea una nueva noticia.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = Permissions.NewsCreate)]
        public async Task<IActionResult> Create([FromBody] NewsCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var news = new News
            {
                Title = dto.Title,
                Slug = News.GenerateSlug(dto.Title),
                Excerpt = dto.Excerpt,
                Content = dto.Content,
                Category = dto.Category,
                Tags = dto.Tags,
                IsFeatured = dto.IsFeatured,
                AllowComments = dto.AllowComments,
                AllowReactions = dto.AllowReactions,
                Status = dto.Status,
                CreatedAt = DateTime.UtcNow
            };

            if (dto.MediaUrls != null)
            {
                foreach (var url in dto.MediaUrls)
                    news.Media.Add(new NewsMedia { Url = url, MediaType = "image" });
            }

            _context.News.Add(news);
            await _context.SaveChangesAsync();

            // ✅ CORRECCIÓN VITAL: Ahora apuntamos a GetById pasándole su ID
            return CreatedAtAction(nameof(GetById), new { id = news.Id }, MapToResponseDto(news));
        }

        /// <summary>
        /// Actualiza una noticia existente.
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Policy = Permissions.NewsManage)]
        public async Task<IActionResult> Update(Guid id, [FromBody] NewsUpdateDto dto)
        {
            var news = await _context.News.Include(n => n.Media).FirstOrDefaultAsync(x => x.Id == id);
            if (news == null) return NotFound();

            news.Title = dto.Title;
            news.Slug = News.GenerateSlug(dto.Title);
            news.Excerpt = dto.Excerpt;
            news.Content = dto.Content;
            news.Category = dto.Category;
            news.Tags = dto.Tags;
            news.IsFeatured = dto.IsFeatured;
            news.AllowComments = dto.AllowComments;
            news.AllowReactions = dto.AllowReactions;
            news.Status = dto.Status;
            news.UpdatedAt = DateTime.UtcNow;

            // Actualización de Media: Eliminamos anteriores y agregamos los nuevos links de Supabase
            if (dto.MediaUrls != null)
            {
                _context.NewsMedia.RemoveRange(news.Media);
                foreach (var url in dto.MediaUrls)
                {
                    news.Media.Add(new NewsMedia { Url = url, MediaType = "image" });
                }
            }

            await _context.SaveChangesAsync();
            return Ok(MapToResponseDto(news));
        }


        /// <summary>
        /// Elimina físicamente una noticia.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = Permissions.NewsManage)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var news = await _context.News.FindAsync(id);
            if (news == null) return NotFound();

            _context.News.Remove(news);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Cambia rápidamente el estado (Publicado/Borrador/Archivado).
        /// </summary>
        [HttpPatch("{id}/status")]
        [Authorize(Policy = Permissions.NewsManage)]
        public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] NewsStatus newStatus)
        {
            var news = await _context.News.FindAsync(id);
            if (news == null) return NotFound();

            news.Status = newStatus;
            news.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Estado actualizado a {newStatus}", status = (int)newStatus });
        }

        // 🛡️ MÉTODO PRIVADO DE MAPEO: Rompe la referencia circular y limpia la respuesta
        private static NewsResponseDto MapToResponseDto(News news)
        {
            return new NewsResponseDto
            {
                Id = news.Id,
                Title = news.Title,
                Excerpt = news.Excerpt,
                Content = news.Content,
                Category = news.Category,
                Tags = news.Tags,
                IsFeatured = news.IsFeatured,
                AllowComments = news.AllowComments,
                AllowReactions = news.AllowReactions,
                Status = news.Status,
                CreatedAt = news.CreatedAt,
                ViewCount = news.ViewCount,
                Slug = news.Slug,
                // Extraemos solo los strings de las URLs para evitar el bucle infinito del JSON
                MediaUrls = news.Media.Select(m => m.Url).ToList()
            };
        }

        /// <summary>
        /// Obtiene una noticia detallada por su Slug (Ideal para el Frontend público).
        /// </summary>
        [HttpGet("{slug}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetBySlug(string slug)
        {
            // Evitamos que entre aquí si por error mandan "id" o "feed"
            if (slug == "feed" || slug == "admin") return NotFound();

            var news = await _context.News.Include(n => n.Media).FirstOrDefaultAsync(n => n.Slug == slug);
            if (news == null) return NotFound(new { message = "La noticia no existe." });

            news.ViewCount++;
            await _context.SaveChangesAsync();
            return Ok(MapToResponseDto(news));
        }

        /// <summary>
        /// Obtiene una noticia detallada por su ID (Ideal para edición en Admin).
        /// </summary>
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(Guid id)
        {
            var news = await _context.News.Include(n => n.Media).FirstOrDefaultAsync(n => n.Id == id);
            if (news == null) return NotFound();
            return Ok(MapToResponseDto(news));
        }


    }
}