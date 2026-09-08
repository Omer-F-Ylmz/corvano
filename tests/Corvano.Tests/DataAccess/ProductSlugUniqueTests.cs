using Corvano.DataAccess.Concrete.EntityFramework;
using Corvano.DataAccess.Concrete.EntityFramework.Contexts;
using Corvano.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace Corvano.Tests.DataAccess;

public sealed class ProductSlugUniqueTests : IAsyncLifetime
{
    private const string LocalDbConnection =
        "Server=(localdb)\\MSSQLLocalDB;Database=CorvanoTest;Trusted_Connection=True;TrustServerCertificate=True";

    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("ConnectionStrings__Default") ?? LocalDbConnection;

    private static DbContextOptions<CorvanoContext> Options => new DbContextOptionsBuilder<CorvanoContext>()
        .UseSqlServer(ConnectionString)
        .Options;

    public async Task InitializeAsync()
    {
        await using var context = new CorvanoContext(Options);
        await context.Database.MigrateAsync();
        await context.Products.ExecuteDeleteAsync();
    }

    public async Task DisposeAsync()
    {
        await using var context = new CorvanoContext(Options);
        await context.Products.ExecuteDeleteAsync();
    }

    [Fact]
    public async Task Adding_second_product_with_same_slug_is_rejected_by_unique_index()
    {
        await using var context = new CorvanoContext(Options);
        var dal = new EfProductDal(context);
        var unitOfWork = new EfUnitOfWork(context);

        await dal.AddAsync(NewProduct("Keten Gömlek", "keten-gomlek"));
        await unitOfWork.SaveChangesAsync();

        await dal.AddAsync(NewProduct("Keten Gömlek (Kopya)", "keten-gomlek"));

        await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWork.SaveChangesAsync());
    }

    [Fact]
    public async Task Products_with_different_slugs_are_both_persisted()
    {
        await using var context = new CorvanoContext(Options);
        var dal = new EfProductDal(context);
        var unitOfWork = new EfUnitOfWork(context);

        await dal.AddAsync(NewProduct("Keten Gömlek", "keten-gomlek"));
        await dal.AddAsync(NewProduct("Yün Pantolon", "yun-pantolon"));
        await unitOfWork.SaveChangesAsync();

        var all = await dal.GetListAsync();

        Assert.Equal(2, all.Count);
    }

    private static Product NewProduct(string name, string slug) => new()
    {
        Name = name,
        Slug = slug,
        Price = 1290m,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };
}
