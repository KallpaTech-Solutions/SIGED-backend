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
        /// <remarks>Accesible por cualquier usuario sin necesidad de login.</remarks>
        [HttpGet("feed")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPublicFeed()
        {
            var news = await _context.News
                .Include(n => n.Media)
                .Where(n => n.Status == NewsStatus.Published)
                .OrderByDescending(n => n.IsFeatured)
                .ThenByDescending(n => n.CreatedAt)
                .ToListAsync();

            return Ok(news);
        }

        /// <summary>
        /// Lista de noticias para administración (Incluye borradores y archivados).
        /// </summary>
        [HttpGet("admin")]
        [Authorize(Policy = Permissions.NewsView)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllAdmin()
        {
            var news = await _context.News
                .Include(n => n.Media)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
            return Ok(news);
        }

        /// <summary>
        /// Crea una nueva noticia con generación automática de Slug.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = Permissions.NewsCreate)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] NewsCreateDto dto)
        {
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

            return CreatedAtAction(nameof(GetPublicFeed), new { id = news.Id }, news);
        }

        /// <summary>
        /// Actualiza o Archiva una noticia existente.
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Policy = Permissions.NewsManage)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] NewsUpdateDto dto)
        {
            var news = await _context.News.Include(n => n.Media).FirstOrDefaultAsync(x => x.Id == id);
            if (news == null) return NotFound();

            news.Title = dto.Title;
            news.Slug = News.GenerateSlug(dto.Title); // Actualiza slug si el título cambió
            news.Excerpt = dto.Excerpt;
            news.Content = dto.Content;
            news.Category = dto.Category;
            news.Tags = dto.Tags;
            news.IsFeatured = dto.IsFeatured;
            news.AllowComments = dto.AllowComments;
            news.AllowReactions = dto.AllowReactions;
            news.Status = dto.Status; // Aquí manejamos "Archivar" cambiando a NewsStatus.Archived
            news.UpdatedAt = DateTime.UtcNow;

            // Simple actualización de media (puedes mejorar esto eliminando/agregando)
            if (dto.MediaUrls != null)
            {
                _context.NewsMedia.RemoveRange(news.Media);
                foreach (var url in dto.MediaUrls)
                    news.Media.Add(new NewsMedia { Url = url, MediaType = "image" });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Noticia actualizada con éxito", slug = news.Slug });
        }

        /// <summary>
        /// Elimina físicamente una noticia y sus archivos asociados.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = Permissions.NewsManage)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var news = await _context.News.FindAsync(id);
            if (news == null) return NotFound();

            _context.News.Remove(news);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        
    }
}
