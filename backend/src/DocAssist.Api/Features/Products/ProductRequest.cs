using System.ComponentModel.DataAnnotations;
using DocAssist.Api.Domain;

namespace DocAssist.Api.Features.Products;

// Lo que el cliente envía para crear (POST) o reemplazar (PUT) un producto.
// No lleva Id: lo genera la base de datos al crear y viene en la URL al modificar.
public sealed record ProductRequest : IValidatableObject
{
    [Required, MaxLength(200)]
    public string Name { get; init; } = "";

    [Required, MaxLength(100)]
    public string Brand { get; init; } = "";

    [Required, MaxLength(200)]
    public string Model { get; init; } = "";

    // Los obligatorios de tipo valor son anulables (?) a propósito: un JSON sin el
    // campo llega como null y [Required] lo rechaza. Si fueran ProductCategory o
    // int, llegarían como Phone o 0 sin que nadie se enterase.
    [Required]
    public ProductCategory? Category { get; init; }

    [Required, Range(0.01, 100_000)]
    public decimal? Price { get; init; }

    [Required, Range(0, 100_000)]
    public int? Stock { get; init; }

    [Required]
    public DateOnly? ReleaseDate { get; init; }

    // Opcionales: null es válido; si vienen, tienen que tener sentido.
    [Range(1, 2048)]
    public int? RamGb { get; init; }

    [Range(1, 100_000)]
    public int? StorageGb { get; init; }

    [Range(1.0, 99.9)]
    public decimal? ScreenInches { get; init; }

    // Reglas que no caben en un atributo. Solo se ejecutan si las de arriba pasan.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Permite productos en reserva, pero no fechas absurdas.
        var latestAllowed = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1);

        if (ReleaseDate > latestAllowed)
        {
            yield return new ValidationResult(
                "The release date cannot be more than one year in the future.",
                [nameof(ReleaseDate)]);
        }
    }

    public Product ToEntity()
    {
        var product = new Product { Name = Name, Brand = Brand, Model = Model };
        ApplyTo(product);
        return product;
    }

    // Los ! son seguros: la validación ya ha comprobado que no son null
    // antes de que el endpoint llegue a llamar a este método.
    public void ApplyTo(Product product)
    {
        product.Name = Name;
        product.Brand = Brand;
        product.Model = Model;
        product.Category = Category!.Value;
        product.Price = Price!.Value;
        product.Stock = Stock!.Value;
        product.ReleaseDate = ReleaseDate!.Value;
        product.RamGb = RamGb;
        product.StorageGb = StorageGb;
        product.ScreenInches = ScreenInches;
    }
}
