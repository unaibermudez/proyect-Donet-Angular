using DocAssist.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocAssist.Api.Data.Configurations;

// Cómo se mapea Product a la tabla. Se mantiene fuera de la entidad
// para que Product no dependa de EF Core.
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(p => p.Name).HasMaxLength(200);
        builder.Property(p => p.Brand).HasMaxLength(100);
        builder.Property(p => p.Model).HasMaxLength(200);

        // Guardar el enum como texto ("Phone") y no como número (0):
        // se lee mejor en la base de datos y no se rompe si se reordena el enum.
        builder.Property(p => p.Category)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(p => p.Price).HasPrecision(10, 2);
        builder.Property(p => p.ScreenInches).HasPrecision(4, 1);

        // El agente filtrará por categoría ("¿qué móviles tenemos...?").
        builder.HasIndex(p => p.Category);
    }
}
