using System.ComponentModel.DataAnnotations;
using DocAssist.Api.Domain;
using DocAssist.Api.Features.Products;

namespace DocAssist.Api.Tests.Features.Products;

// Tests unitarios de las reglas de validación de ProductRequest.
// No arrancan la API: ejecutan los mismos atributos con el Validator de .NET.
public class ProductRequestValidationTests
{
    [Fact]
    public void ValidRequest_HasNoErrors()
    {
        var errors = Validate(ValidRequest());

        Assert.Empty(errors);
    }

    [Fact]
    public void EmptyRequest_ReportsEveryRequiredField()
    {
        var invalidMembers = InvalidMembers(new ProductRequest()).Order();

        string[] expected = ["Brand", "Category", "Model", "Name", "Price", "ReleaseDate", "Stock"];
        Assert.Equal(expected, invalidMembers);
    }

    [Theory]
    [MemberData(nameof(RequestsWithOneInvalidField))]
    public void InvalidField_IsReported(ProductRequest request, string expectedMember)
    {
        var member = Assert.Single(InvalidMembers(request));

        Assert.Equal(expectedMember, member);
    }

    [Fact]
    public void MissingOptionalSpecs_AreValid()
    {
        // Una consola de sobremesa: sin pantalla y sin más especificaciones.
        var request = ValidRequest() with { RamGb = null, StorageGb = null, ScreenInches = null };

        Assert.Empty(Validate(request));
    }

    [Fact]
    public void ReleaseDateWithinOneYear_IsValid()
    {
        // Un producto en reserva, que sale dentro de unos meses.
        var request = ValidRequest() with { ReleaseDate = Today().AddMonths(6) };

        Assert.Empty(Validate(request));
    }

    // Cada caso parte de una petición válida y estropea un único campo.
    public static TheoryData<ProductRequest, string> RequestsWithOneInvalidField => new()
    {
        { ValidRequest() with { Name = "   " }, nameof(ProductRequest.Name) },
        { ValidRequest() with { Name = new string('x', 201) }, nameof(ProductRequest.Name) },
        { ValidRequest() with { Price = 0m }, nameof(ProductRequest.Price) },
        { ValidRequest() with { Price = -50m }, nameof(ProductRequest.Price) },
        { ValidRequest() with { Stock = -3 }, nameof(ProductRequest.Stock) },
        { ValidRequest() with { RamGb = 0 }, nameof(ProductRequest.RamGb) },
        { ValidRequest() with { ScreenInches = -1m }, nameof(ProductRequest.ScreenInches) },
        { ValidRequest() with { ReleaseDate = Today().AddYears(2) }, nameof(ProductRequest.ReleaseDate) }
    };

    private static ProductRequest ValidRequest() => new()
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
    };

    private static List<ValidationResult> Validate(ProductRequest request)
    {
        var results = new List<ValidationResult>();

        // validateAllProperties: true es imprescindible. Sin él, solo se comprueba
        // [Required] y se ignoran [Range], [MaxLength]...
        Validator.TryValidateObject(
            request, new ValidationContext(request), results, validateAllProperties: true);

        return results;
    }

    private static IEnumerable<string> InvalidMembers(ProductRequest request) =>
        Validate(request).SelectMany(result => result.MemberNames);

    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow);
}
