using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DocAssist.Api.Tests;

// Tests de integración: arrancan la API completa en memoria y la llaman por HTTP.
public class SystemEndpointsTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Info_ReturnsNameAndEnvironment()
    {
        var client = factory.CreateClient();

        var info = await client.GetFromJsonAsync<AppInfo>("/api/info");

        Assert.NotNull(info);
        Assert.Equal("DocAssist", info.Name);
        Assert.Equal("Development", info.Environment);
    }

    [Fact]
    public async Task OpenApi_IsNotExposed_OutsideDevelopment()
    {
        var client = factory
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"))
            .CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Copia local del contrato de /api/info: el test comprueba el JSON tal como
    // lo vería un cliente, sin depender de los tipos internos de la API.
    private sealed record AppInfo(string Name, string Environment, string DotnetVersion);
}
