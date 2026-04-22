using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Siged.Domain.Entities.Core.Tournaments;

namespace Siged.Infrastructure.Persistence.Configurations
{
    public class VenueConfiguration : IEntityTypeConfiguration<Venue>
    {
        public void Configure(EntityTypeBuilder<Venue> builder)
        {
            builder.ToTable("Venues");

            builder.HasOne(v => v.Complex)
                .WithMany(c => c.Venues)
                .HasForeignKey(v => v.ComplexId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
