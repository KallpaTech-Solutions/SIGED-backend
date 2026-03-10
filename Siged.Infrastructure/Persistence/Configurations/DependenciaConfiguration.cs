using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Siged.Domain.Entities.Core;

namespace Siged.Infrastructure.Persistence.Configurations
{
    public class DependenciaConfiguration : IEntityTypeConfiguration<Dependencia>
    {
        public void Configure(EntityTypeBuilder<Dependencia> builder)
        {
            builder.ToTable("Dependencias");

            builder.HasKey(d => d.Id);
            builder.Property(d => d.Nombre).IsRequired().HasMaxLength(100);
            builder.Property(d => d.Siglas).HasMaxLength(20);
            builder.HasMany(d => d.Administradores).WithOne(a => a.Dependencia).HasForeignKey(a => a.DependenciaId).OnDelete(DeleteBehavior.Restrict);

        }
    }
}