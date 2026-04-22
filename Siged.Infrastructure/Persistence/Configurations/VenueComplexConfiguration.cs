using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Siged.Domain.Entities.Core.Tournaments;

namespace Siged.Infrastructure.Persistence.Configurations
{
    public class VenueComplexConfiguration : IEntityTypeConfiguration<VenueComplex>
    {
        public void Configure(EntityTypeBuilder<VenueComplex> builder)
        {
            builder.ToTable("VenueComplexes");
        }
    }
}
