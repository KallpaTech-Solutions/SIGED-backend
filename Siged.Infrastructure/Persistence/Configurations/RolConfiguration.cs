using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Siged.Domain.Entities.Security;

public class RolConfiguration : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> builder)
    {
        builder.ToTable("Roles");

        builder.HasMany(r => r.Permisos)
               .WithMany(p => p.Roles)
               .UsingEntity<Dictionary<string, object>>(
                   "RolPermisos",
                   j => j.HasOne<Permiso>().WithMany().HasForeignKey("PermisosIdPermiso"),
                   j => j.HasOne<Rol>().WithMany().HasForeignKey("RolesId"));
    }
}