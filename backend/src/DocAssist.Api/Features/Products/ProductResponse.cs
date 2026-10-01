using DocAssist.Api.Domain;

namespace DocAssist.Api.Features.Products;

// Lo que la API devuelve de un producto.
public sealed record ProductResponse(
    int Id,
    string Name,
    string Brand,
    string Model,
    ProductCategory Category,
    decimal Price,
    int Stock,
    DateOnly ReleaseDate,
    int? RamGb,
    int? StorageGb,
    decimal? ScreenInches)
{
    public static ProductResponse FromEntity(Product product) => new(
        product.Id,
        product.Name,
        product.Brand,
        product.Model,
        product.Category,
        product.Price,
        product.Stock,
        product.ReleaseDate,
        product.RamGb,
        product.StorageGb,
        product.ScreenInches);
}
