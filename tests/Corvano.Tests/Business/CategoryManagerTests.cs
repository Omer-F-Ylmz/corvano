using System.Net;
using Corvano.Business.Concrete;
using Corvano.DataAccess.Concrete.EntityFramework;
using Corvano.DataAccess.Concrete.EntityFramework.Contexts;
using Corvano.Entities.Concrete;

namespace Corvano.Tests.Business;

[Collection(DatabaseCollection.Name)]
public sealed class CategoryManagerTests : IAsyncLifetime
{
    public Task InitializeAsync() => TestDb.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static CategoryManager NewService(CorvanoContext context)
        => new(new EfCategoryDal(context), new EfProductDal(context), new EfUnitOfWork(context));

    [Fact]
    public async Task Second_category_with_the_same_name_gets_a_suffixed_slug()
    {
        await using var context = TestDb.NewContext();
        var service = NewService(context);

        await service.AddAsync(new Category { Name = "Gömlek", IsActive = true });
        await service.AddAsync(new Category { Name = "Gömlek", IsActive = true });

        var (_, listed) = await service.GetAllAsync();
        var slugs = listed.Data!.Select(c => c.Slug).OrderBy(s => s, StringComparer.Ordinal).ToList();

        Assert.Equal(new[] { "gomlek", "gomlek-2" }, slugs);
    }

    [Fact]
    public async Task Deleting_a_category_that_still_has_products_returns_conflict()
    {
        await using var context = TestDb.NewContext();
        var service = NewService(context);
        await service.AddAsync(new Category { Name = "Pantolon", IsActive = true });
        var (_, listed) = await service.GetAllAsync();
        var categoryId = listed.Data![0].Id;

        var productDal = new EfProductDal(context);
        await productDal.AddAsync(new Product
        {
            Name = "Yün Pantolon",
            Slug = "yun-pantolon",
            Description = "Kışlık.",
            CategoryId = categoryId,
            Price = 2490m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await new EfUnitOfWork(context).SaveChangesAsync();

        var (status, result) = await service.DeleteAsync(categoryId);

        Assert.Equal(HttpStatusCode.Conflict, status);
        Assert.False(result.Success);
        var (_, afterDelete) = await service.GetAllAsync();
        Assert.Single(afterDelete.Data!);
    }

    [Fact]
    public async Task Deleting_an_empty_category_removes_it()
    {
        await using var context = TestDb.NewContext();
        var service = NewService(context);
        await service.AddAsync(new Category { Name = "Aksesuar", IsActive = true });
        var (_, listed) = await service.GetAllAsync();

        var (status, result) = await service.DeleteAsync(listed.Data![0].Id);

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.True(result.Success);
        var (_, afterDelete) = await service.GetAllAsync();
        Assert.Empty(afterDelete.Data!);
    }
}
