using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Siged.Domain.Entities.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Siged.Infrastructure.Persistence.Configurations
{
    public class EncargadoConfiguration : IEntityTypeConfiguration<Encargado>
    {
        public void Configure(EntityTypeBuilder<Encargado> builder)
        {
            // El cargo del profesor Gregorio es obligatorio para el sistema
            builder.Property(en => en.Cargo)
                .IsRequired()
                .HasMaxLength(100);
        }
    }
}
