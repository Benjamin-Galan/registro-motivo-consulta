using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RegistroConsultas.Domain.Entities;
using RegistroConsultas.Infrastructure.Identity;

namespace RegistroConsultas.Infrastructure.Persistence;

public sealed class RegistroDbContext(DbContextOptions<RegistroDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<ZonaFilial> ZonasFiliales => Set<ZonaFilial>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Area>(entity =>
        {
            entity.ToTable("areas");
            entity.HasKey(area => area.Id);
            entity.Property(area => area.Id).HasColumnName("id").UseIdentityColumn();
            entity.Property(area => area.CodigoArea).HasColumnName("codigo_area").HasMaxLength(20).IsRequired();
            entity.Property(area => area.Nombre).HasColumnName("nombre").HasMaxLength(120).IsRequired();
            entity.Property(area => area.RegAnulado).HasColumnName("reg_anulado").IsRequired();
            entity.Property(area => area.CreadoEl).HasColumnName("creado_el").HasColumnType("datetimeoffset").IsRequired();
            entity.Property(area => area.ModificadoEl).HasColumnName("modificado_el").HasColumnType("datetimeoffset");
            entity.HasIndex(area => area.CodigoArea).IsUnique().HasDatabaseName("UX_areas_codigo_area");
        });

        builder.Entity<ZonaFilial>(entity =>
        {
            entity.ToTable("zonas_filiales");
            entity.HasKey(zona => zona.Id);
            entity.Property(zona => zona.Id).HasColumnName("id").UseIdentityColumn();
            entity.Property(zona => zona.CodigoZona).HasColumnName("codigo_zona").HasMaxLength(20).IsRequired();
            entity.Property(zona => zona.Nombre).HasColumnName("nombre").HasMaxLength(120).IsRequired();
            entity.Property(zona => zona.RegAnulado).HasColumnName("reg_anulado").IsRequired();
            entity.Property(zona => zona.CreadoEl).HasColumnName("creado_el").HasColumnType("datetimeoffset").IsRequired();
            entity.Property(zona => zona.ModificadoEl).HasColumnName("modificado_el").HasColumnType("datetimeoffset");
            entity.HasIndex(zona => zona.CodigoZona).IsUnique().HasDatabaseName("UX_zonas_filiales_codigo_zona");
        });
    }
}
