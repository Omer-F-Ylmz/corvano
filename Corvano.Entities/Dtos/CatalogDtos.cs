using Corvano.Entities.Concrete;

namespace Corvano.Entities.Dtos;

/// <summary>Kart; <paramref name="CategorySlug"/> ürünün üst kategorisidir (görsel yoksa silüet seçimi için).</summary>
public sealed record ProductCardDto(Product Product, string CategorySlug, List<ProductImage> Images, List<ProductVariant> Variants);

public enum CollectionSort
{
    Newest,
    PriceAscending,
    PriceDescending
}

public sealed record CollectionPageDto(
    Category Category,
    List<Category> Subcategories,
    Category? ActiveSubcategory,
    CollectionSort Sort,
    List<ProductCardDto> Products);

public sealed record ProductPageDto(
    Product Product,
    Category Category,
    Category? ParentCategory,
    List<ProductImage> Images,
    List<ProductVariant> Variants);

public sealed record HomeCategoryDto(Category Category, ProductCardDto? Sample, int ProductCount);
