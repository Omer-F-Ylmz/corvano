using Corvano.DataAccess.Concrete.EntityFramework;
using Corvano.DataAccess.Concrete.EntityFramework.Contexts;
using Corvano.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace Corvano.Tests.DataAccess;

[Collection(DatabaseCollection.Name)]
public sealed class ProductSlugUniqueTests : IAsyncLifetime
{
    public Task InitializeAsync() => TestDb.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task<int> SeedCategoryAsync(CorvanoContext context)
    {
        var category = new Category { Name = "Gömlek", Slug = "gomlek", SortOrder = 1, IsActive = true };
        await new EfCategoryDal(context).AddAsync(category);
        await new EfUnitOfWork(context).SaveChangesAsync();
        return category.Id;
    }

    [Fact]
    public async Task Adding_second_product_with_same_slug_is_rejected_by_unique_index()
    {
        await using var context = TestDb.NewContext();
        var categoryId = await SeedCategoryAsync(context);
        var dal = new EfProductDal(context);
        var unitOfWork = new EfUnitOfWork(context);

        await dal.AddAsync(NewProduct(categoryId, "Keten Gömlek", "keten-gomlek"));
        await unitOfWork.SaveChangesAsync();

        await dal.AddAsync(NewProduct(categoryId, "Keten Gömlek (Kopya)", "keten-gomlek"));

        await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWork.SaveChangesAsync());
    }

    [Fact]
    public async Task Products_with_different_slugs_are_both_persisted()
    {
        await using var context = TestDb.NewContext();
        var categoryId = await SeedCategoryAsync(context);
        var dal = new EfProductDal(context);
        var unitOfWork = new EfUnitOfWork(context);

        await dal.AddAsync(NewProduct(categoryId, "Keten Gömlek", "keten-gomlek"));
        await dal.AddAsync(NewProduct(categoryId, "Yün Pantolon", "yun-pantolon"));
        await unitOfWork.SaveChangesAsync();

        var all = await dal.GetListAsync();

        Assert.Equal(2, all.Count);
    }

    private static Product NewProduct(int categoryId, string name, string slug) => new()
    {
        Name = name,
        Slug = slug,
        Description = "Pamuk karışımı.",
        CategoryId = categoryId,
        Price = 1290m,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
