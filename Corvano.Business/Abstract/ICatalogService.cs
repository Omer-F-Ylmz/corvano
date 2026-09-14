using System.Net;
using Corvano.Core.Utilities.Results;
using Corvano.Entities.Dtos;

namespace Corvano.Business.Abstract;

/// <summary>Vitrin okumaları: yalnız yayındaki kategori ve ürünler görünür.</summary>
public interface ICatalogService
{
    /// <summary>Üst kategori sayfası; <paramref name="subcategorySlug"/> verilirse yalnız o alt kategorinin ürünleri.</summary>
    Task<(HttpStatusCode, IDataResult<CollectionPageDto>)> GetCollectionAsync(string slug, string? subcategorySlug, CancellationToken cancellationToken = default);

    Task<(HttpStatusCode, IDataResult<ProductPageDto>)> GetProductAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>Ana sayfa şeritleri: ilk üç üst kategori ve her birinden iki görsel.</summary>
    Task<(HttpStatusCode, IDataResult<List<HomeStripDto>>)> GetHomeStripsAsync(CancellationToken cancellationToken = default);
}
