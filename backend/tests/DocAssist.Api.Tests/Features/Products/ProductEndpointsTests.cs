using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DocAssist.Api.Domain;
using DocAssist.Api.Features.Products;
using DocAssist.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DocAssist.Api.Tests.Features.Products;

// Tests de integración: llaman a /api/products por HTTP y la API escribe en un
// Postgres real (Testcontainers). Cada test crea sus propios datos y no depende
// del orden de ejecución ni del número de productos que haya.
public class ProductEndpointsTests(PostgresApiFactory factory) : IClassFixture<PostgresApiFactory>
{
    // La API envía y recibe los enums como texto ("Console"); el cliente de los
    // tests tiene que hacer lo mismo.
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_ReturnsCreatedWithLocation_AndProductCanBeRead()
    {
        var request = NewRequest("Steam Deck OLED 512 GB");

        var response = await _client.PostAsJsonAsync("/api/products", request, Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ProductResponse>(Json);
        Assert.NotNull(created);
        Assert.Equal($"/api/products/{created.Id}", response.Headers.Location?.ToString());

        var read = await _client.GetFromJsonAsync<ProductResponse>(response.Headers.Location, Json);
        Assert.Equal(created, read);
    }

    [Fact]
    public async Task Create_WithEmptyBody_Returns400WithValidationErrors()
    {
        var response = await _client.PostAsJsonAsync("/api/products", new { }, Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Json);
        Assert.NotNull(problem);
        Assert.Contains(problem.Errors.Keys, key => key.Equals("Category", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404WithProblemDetails()
    {
        var response = await _client.GetAsync("/api/products/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Update_ReplacesTheStoredValues()
    {
        var created = await CreateAsync(NewRequest("Steam Deck LCD 256 GB"));
        var changes = NewRequest("Steam Deck LCD 256 GB") with { Price = 349.00m, Stock = 1 };

        var response = await _client.PutAsJsonAsync($"/api/products/{created.Id}", changes, Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var read = await _client.GetFromJsonAsync<ProductResponse>($"/api/products/{created.Id}", Json);
        Assert.NotNull(read);
        Assert.Equal(349.00m, read.Price);
        Assert.Equal(1, read.Stock);
    }

    [Fact]
    public async Task Delete_RemovesTheProduct_AndSecondDeleteReturns404()
    {
        var created = await CreateAsync(NewRequest("Producto para borrar"));

        var firstDelete = await _client.DeleteAsync($"/api/products/{created.Id}");
        var getAfterDelete = await _client.GetAsync($"/api/products/{created.Id}");
        var secondDelete = await _client.DeleteAsync($"/api/products/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, firstDelete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getAfterDelete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, secondDelete.StatusCode);
    }

    [Fact]
    public async Task List_FilteredByCategory_ReturnsOnlyThatCategory()
    {
        var console = await CreateAsync(NewRequest("Consola para el filtro"));
        var phone = await CreateAsync(NewRequest("Móvil para el filtro") with
        {
            Category = ProductCategory.Phone
        });

        var consoles = await _client.GetFromJsonAsync<List<ProductResponse>>(
            "/api/products?category=Console", Json);

        Assert.NotNull(consoles);
        Assert.All(consoles, product => Assert.Equal(ProductCategory.Console, product.Category));
        Assert.Contains(consoles, product => product.Id == console.Id);
        Assert.DoesNotContain(consoles, product => product.Id == phone.Id);
    }

    private static ProductRequest NewRequest(string name) => new()
    {
        Name = name,
        Brand = "Valve",
        Model = "Steam Deck",
        Category = ProductCategory.Console,
        Price = 569.00m,
        Stock = 4,
        ReleaseDate = new DateOnly(2023, 11, 16),
        RamGb = 16,
        StorageGb = 512,
        ScreenInches = 7.4m
    };

    private async Task<ProductResponse> CreateAsync(ProductRequest request)
    {
        var response = await _client.PostAsJsonAsync("/api/products", request, Json);
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<ProductResponse>(Json);
        return created ?? throw new InvalidOperationException("La API no devolvió el producto creado.");
    }
}
