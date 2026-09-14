using System.Net;
using Corvano.Core.Utilities.Results;
using Corvano.Entities.Concrete;
using Corvano.Entities.Dtos;

namespace Corvano.Business.Abstract;

/// <summary>Vitrin okumaları: yalnız yayındaki kategori ve ürünler görünür.</summary>
public interface ICatalogService
{
    /// <summary>"tumu" slug'ı tüm yayındaki ürünleri döndürür.</summary>
    public const string AllProductsSlug = "tumu";

    /// <summary>Üst kategori sayfası; <paramref name="subcategorySlug"/> verilirse yalnız o alt kategorinin ürünleri.</summary>
    Task<(HttpStatusCode, IDataResult<CollectionPageDto>)> GetCollectionAsync(
        string slug,
        string? subcategorySlug,
        CollectionSort sort = CollectionSort.Newest,
        CancellationToken cancellationToken = default);

    Task<(HttpStatusCode, IDataResult<ProductPageDto>)> GetProductAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>Üst bar menüsü: yayındaki üst kategoriler, sort_order sırasıyla.</summary>
    Task<(HttpStatusCode, IDataResult<List<Category>>)> GetMenuAsync(CancellationToken cancellationToken = default);

    /// <summary>Öne çıkanlar: en yeni <paramref name="count"/> yayındaki ürün.</summary>
    Task<(HttpStatusCode, IDataResult<List<ProductCardDto>>)> GetFeaturedAsync(int count, CancellationToken cancellationToken = default);

    /// <summary>Aynı kategoriden, ürünün kendisi hariç en fazla <paramref name="count"/> yayındaki ürün.</summary>
    Task<(HttpStatusCode, IDataResult<List<ProductCardDto>>)> GetSimilarAsync(string slug, int count, CancellationToken cancellationToken = default);

    /// <summary>Ana sayfa kategori kartları: her üst kategori, örnek ürünü ve alt kategorileriyle birlikte parça sayısı.</summary>
    Task<(HttpStatusCode, IDataResult<List<HomeCategoryDto>>)> GetHomeCategoriesAsync(CancellationToken cancellationToken = default);
}
