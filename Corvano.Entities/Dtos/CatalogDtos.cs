using Corvano.Entities.Concrete;

namespace Corvano.Entities.Dtos;

public sealed record ProductCardDto(Product Product, List<ProductImage> Images, List<ProductVariant> Variants);

public sealed record CollectionPageDto(
    Category Category,
    List<Category> Subcategories,
    Category? ActiveSubcategory,
    List<ProductCardDto> Products);

public sealed record ProductPageDto(
    Product Product,
    Category Category,
    Category? ParentCategory,
    List<ProductImage> Images,
    List<ProductVariant> Variants);

public sealed record HomeStripDto(Category Category, List<ProductImage> Images);
