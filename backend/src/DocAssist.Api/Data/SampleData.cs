using DocAssist.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DocAssist.Api.Data;

// Datos de ejemplo para desarrollo. Los precios son orientativos (precio de
// lanzamiento aproximado en España), no un catálogo real.
public static class SampleData
{
    // Versión síncrona: la usa `dotnet ef database update`.
    public static void Seed(DbContext context)
    {
        var products = context.Set<Product>();
        if (products.Any())
        {
            return;
        }

        products.AddRange(CreateProducts());
        context.SaveChanges();
    }

    // Versión asíncrona: la usa la aplicación al migrar desde código.
    public static async Task SeedAsync(DbContext context, CancellationToken cancellationToken)
    {
        var products = context.Set<Product>();
        if (await products.AnyAsync(cancellationToken))
        {
            return;
        }

        products.AddRange(CreateProducts());
        await context.SaveChangesAsync(cancellationToken);
    }

    private static List<Product> CreateProducts() =>
    [
        // ---------- Móviles ----------
        new()
        {
            Name = "Samsung Galaxy S25 Ultra 256 GB",
            Brand = "Samsung",
            Model = "Galaxy S25 Ultra",
            Category = ProductCategory.Phone,
            Price = 1459.00m,
            Stock = 8,
            ReleaseDate = new DateOnly(2025, 2, 7),
            RamGb = 12,
            StorageGb = 256,
            ScreenInches = 6.9m
        },
        new()
        {
            Name = "Apple iPhone 16 Pro 256 GB",
            Brand = "Apple",
            Model = "iPhone 16 Pro",
            Category = ProductCategory.Phone,
            Price = 1349.00m,
            Stock = 5,
            ReleaseDate = new DateOnly(2024, 9, 20),
            RamGb = 8,
            StorageGb = 256,
            ScreenInches = 6.3m
        },
        new()
        {
            Name = "Google Pixel 9 128 GB",
            Brand = "Google",
            Model = "Pixel 9",
            Category = ProductCategory.Phone,
            Price = 899.00m,
            Stock = 12,
            ReleaseDate = new DateOnly(2024, 8, 22),
            RamGb = 12,
            StorageGb = 128,
            ScreenInches = 6.3m
        },
        new()
        {
            Name = "Xiaomi Redmi Note 14 Pro 256 GB",
            Brand = "Xiaomi",
            Model = "Redmi Note 14 Pro",
            Category = ProductCategory.Phone,
            Price = 399.99m,
            Stock = 20,
            ReleaseDate = new DateOnly(2025, 1, 15),
            RamGb = 8,
            StorageGb = 256,
            ScreenInches = 6.7m
        },

        // ---------- Ordenadores ----------
        new()
        {
            Name = "Apple MacBook Air 13\" M3",
            Brand = "Apple",
            Model = "MacBook Air M3",
            Category = ProductCategory.Computer,
            Price = 1299.00m,
            Stock = 6,
            ReleaseDate = new DateOnly(2024, 3, 8),
            RamGb = 16,
            StorageGb = 256,
            ScreenInches = 13.6m
        },
        new()
        {
            Name = "ASUS ROG Strix G16",
            Brand = "ASUS",
            Model = "ROG Strix G16",
            Category = ProductCategory.Computer,
            Price = 1799.00m,
            Stock = 3,
            ReleaseDate = new DateOnly(2024, 2, 1),
            RamGb = 16,
            StorageGb = 1024,
            ScreenInches = 16.0m
        },
        new()
        {
            // Sobremesa: no tiene pantalla integrada.
            Name = "HP Victus 15L",
            Brand = "HP",
            Model = "Victus 15L",
            Category = ProductCategory.Computer,
            Price = 999.00m,
            Stock = 4,
            ReleaseDate = new DateOnly(2023, 6, 1),
            RamGb = 16,
            StorageGb = 1024,
            ScreenInches = null
        },

        // ---------- Consolas ----------
        new()
        {
            Name = "Sony PlayStation 5 Slim",
            Brand = "Sony",
            Model = "PS5 Slim",
            Category = ProductCategory.Console,
            Price = 549.99m,
            Stock = 0,
            ReleaseDate = new DateOnly(2023, 11, 10),
            RamGb = 16,
            StorageGb = 1024,
            ScreenInches = null
        },
        new()
        {
            Name = "Nintendo Switch 2",
            Brand = "Nintendo",
            Model = "Switch 2",
            Category = ProductCategory.Console,
            Price = 469.99m,
            Stock = 10,
            ReleaseDate = new DateOnly(2025, 6, 5),
            RamGb = 12,
            StorageGb = 256,
            ScreenInches = 7.9m
        },
        new()
        {
            Name = "Microsoft Xbox Series S 512 GB",
            Brand = "Microsoft",
            Model = "Xbox Series S",
            Category = ProductCategory.Console,
            Price = 299.99m,
            Stock = 7,
            ReleaseDate = new DateOnly(2020, 11, 10),
            RamGb = 10,
            StorageGb = 512,
            ScreenInches = null
        }
    ];
}
