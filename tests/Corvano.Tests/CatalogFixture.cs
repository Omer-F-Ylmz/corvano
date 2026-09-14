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

    public static async Task<int> AddCategoryAsync(string name, string slug, int sortOrder, bool isActive = true, int? parentId = null)
    {
        await using var context = TestDb.NewContext();
        var category = new Category { Name = name, Slug = slug, SortOrder = sortOrder, IsActive = isActive, ParentId = parentId };
        await new EfCategoryDal(context).AddAsync(category);
        await new EfUnitOfWork(context).SaveChangesAsync();
        return category.Id;
    }

    public static async Task<int> AddProductAsync(int categoryId, string name, string slug, decimal price = 890m, bool isActive = true, int ageMinutes = 0, bool isFeatured = false)
    {
        await using var context = TestDb.NewContext();
        return await AddProductAsync(context, categoryId, name, slug, isActive, price, ageMinutes, isFeatured);
    }

    public static async Task<int> AddVariantAsync(int productId, string size, int stock, decimal? priceOverride = null)
    {
        await using var context = TestDb.NewContext();
        var variant = new ProductVariant
        {
            ProductId = productId,
            Size = size,
            Color = "Ekru",
            Sku = $"T-{productId}-{size}",
            Stock = stock,
            PriceOverride = priceOverride
        };
        await new EfProductVariantDal(context).AddAsync(variant);
        await new EfUnitOfWork(context).SaveChangesAsync();
        return variant.Id;
    }

    private static async Task<int> AddProductAsync(CorvanoContext context, int categoryId, string name, string slug, bool isActive, decimal price = 890m, int ageMinutes = 0, bool isFeatured = false)
    {
        var createdAt = DateTime.UtcNow.AddMinutes(-ageMinutes);
        var product = new Product
        {
            Name = name,
            Slug = slug,
            Description = "%100 ipek | Türkiye | Kuru temizleme",
            CategoryId = categoryId,
            Price = price,
            IsActive = isActive,
            IsFeatured = isFeatured,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
        await new EfProductDal(context).AddAsync(product);
        await new EfUnitOfWork(context).SaveChangesAsync();
        return product.Id;
    }
}
