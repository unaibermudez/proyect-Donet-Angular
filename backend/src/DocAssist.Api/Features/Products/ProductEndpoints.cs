using DocAssist.Api.Data;
using DocAssist.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace DocAssist.Api.Features.Products;

public static class ProductEndpoints
{
    // Categoría de los logs de este archivo. Una clase static no se puede usar en
    // ILogger<T>, así que el logger se pide a ILoggerFactory por nombre.
    private const string LogCategory = "DocAssist.Api.Products";

    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products").WithTags("Products");

        group.MapGet("/", GetProducts)
            .WithSummary("Lista los productos, opcionalmente filtrados por categoría");

        group.MapGet("/{id:int}", GetProductById)
            .WithSummary("Obtiene un producto por su id");

        group.MapPost("/", CreateProduct)
            .WithSummary("Crea un producto");

        group.MapPut("/{id:int}", UpdateProduct)
            .WithSummary("Reemplaza todos los datos de un producto");

        group.MapDelete("/{id:int}", DeleteProduct)
            .WithSummary("Borra un producto");

        return app;
    }

    private static async Task<Ok<List<ProductResponse>>> GetProducts(
        ProductCategory? category,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        // Solo lectura: EF Core no necesita vigilar cambios en estas entidades.
        var query = db.Products.AsNoTracking();

        if (category is not null)
        {
            query = query.Where(p => p.Category == category.Value);
        }

        var products = await query
            .OrderBy(p => p.Name)
            .Select(p => ProductResponse.FromEntity(p))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(products);
    }

    private static async Task<Results<Ok<ProductResponse>, NotFound>> GetProductById(
        int id,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var product = await db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return product is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(ProductResponse.FromEntity(product));
    }

    private static async Task<Created<ProductResponse>> CreateProduct(
        ProductRequest request,
        AppDbContext db,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var product = request.ToEntity();

        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);

        loggerFactory.CreateLogger(LogCategory).LogInformation(
            "Product {ProductId} created: {ProductName} ({Category})",
            product.Id, product.Name, product.Category);

        // Tras SaveChanges, EF Core ha rellenado product.Id con el que generó Postgres.
        return TypedResults.Created(
            $"/api/products/{product.Id}",
            ProductResponse.FromEntity(product));
    }

    private static async Task<Results<Ok<ProductResponse>, NotFound>> UpdateProduct(
        int id,
        ProductRequest request,
        AppDbContext db,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        // Sin AsNoTracking: queremos que EF Core detecte los cambios y los guarde.
        var product = await db.Products.FindAsync([id], cancellationToken);
        if (product is null)
        {
            return TypedResults.NotFound();
        }

        request.ApplyTo(product);
        await db.SaveChangesAsync(cancellationToken);

        loggerFactory.CreateLogger(LogCategory).LogInformation(
            "Product {ProductId} updated", product.Id);

        return TypedResults.Ok(ProductResponse.FromEntity(product));
    }

    private static async Task<Results<NoContent, NotFound>> DeleteProduct(
        int id,
        AppDbContext db,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        // Un único DELETE en la base de datos, sin cargar antes el producto.
        var deletedRows = await db.Products
            .Where(p => p.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        if (deletedRows == 0)
        {
            return TypedResults.NotFound();
        }

        loggerFactory.CreateLogger(LogCategory).LogInformation(
            "Product {ProductId} deleted", id);

        return TypedResults.NoContent();
    }
}
