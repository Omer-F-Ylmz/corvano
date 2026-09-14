using Corvano.DataAccess.Concrete.EntityFramework;
using Corvano.DataAccess.Concrete.EntityFramework.Contexts;
using Corvano.Entities.Concrete;

namespace Corvano.Tests;

/// <summary>Aksesuar › (Kravat, Yaka Çiçeği) ağacı; bir kravat pasif.</summary>
public static class CatalogFixture
{
    public const string ActiveTie = "kiremit-kravat";
    public const string PassiveTie = "eski-kravat";
    public const string Boutonniere = "gri-yaka-cicegi";

    public static async Task SeedAsync()
    {
        await using var context = TestDb.NewContext();
        var categories = new EfCategoryDal(context);
        var unitOfWork = new EfUnitOfWork(context);

        var accessories = new Category { Name = "Aksesuar", Slug = "aksesuar", SortOrder = 5, IsActive = true };
        await categories.AddAsync(accessories);
        await unitOfWork.SaveChangesAsync();

        var ties = new Category { Name = "Kravat", Slug = "kravat", ParentId = accessories.Id, SortOrder = 2, IsActive = true };
        var boutonnieres = new Category { Name = "Yaka Çiçeği", Slug = "yaka-cicegi", ParentId = accessories.Id, SortOrder = 1, IsActive = true };
        await categories.AddAsync(ties);
        await categories.AddAsync(boutonnieres);
        await unitOfWork.SaveChangesAsync();

        await AddProductAsync(context, ties.Id, "Kiremit Kravat", ActiveTie, isActive: true);
        await AddProductAsync(context, ties.Id, "Eski Kravat", PassiveTie, isActive: false);
        await AddProductAsync(context, boutonnieres.Id, "Gri Yaka Çiçeği", Boutonniere, isActive: true);
    }

    private static async Task AddProductAsync(CorvanoContext context, int categoryId, string name, string slug, bool isActive)
    {
        var product = new Product
        {
            Name = name,
            Slug = slug,
            Description = "%100 ipek | Türkiye | Kuru temizleme",
            CategoryId = categoryId,
            Price = 890m,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await new EfProductDal(context).AddAsync(product);
        await new EfUnitOfWork(context).SaveChangesAsync();
    }
}
