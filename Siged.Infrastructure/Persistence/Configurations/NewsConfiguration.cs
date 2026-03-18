using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Siged.Domain.Entities.Core.Notice;

namespace Siged.Infrastructure.Persistence.Configurations
{
    internal class NewsConfiguration : IEntityTypeConfiguration<News>
    {
        public void Configure(EntityTypeBuilder<News> builder)
        {
            builder.ToTable("News");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(120);

            // Índice único para búsquedas rápidas por URL
            builder.Property(x => x.Slug)
                .IsRequired()
                .HasMaxLength(150);

            builder.HasIndex(x => x.Slug)
                .IsUnique();

            builder.Property(x => x.Excerpt)
                .IsRequired()
                .HasMaxLength(250);

            builder.Property(x => x.Content)
                .IsRequired();

            // Configuración de Enums como texto en la DB
            builder.Property(x => x.Category)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(NewsStatus.Draft);

            builder.Property(x => x.Tags)
                .HasMaxLength(500);

            // Relación 1 a N con NewsMedia
            builder.HasMany(x => x.Media)
                .WithOne(m => m.News)
                .HasForeignKey(m => m.NewsId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(x => x.CreatedAt)
                .IsRequired();
        }
    }
}