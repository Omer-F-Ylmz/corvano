using System.Net;
using Corvano.Core.Utilities.Results;
using Corvano.Entities.Concrete;

namespace Corvano.Business.Abstract;

public interface IProductService
{
    Task<(HttpStatusCode, IDataResult<List<Product>>)> GetAllAsync(CancellationToken cancellationToken = default);
    Task<(HttpStatusCode, IDataResult<Product>)> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(HttpStatusCode, IDataResult<Product>)> AddAsync(Product product, CancellationToken cancellationToken = default);
    Task<(HttpStatusCode, IResult)> UpdateAsync(Product product, CancellationToken cancellationToken = default);

    /// <summary>Ürün silinmez, pasife alınır.</summary>
    Task<(HttpStatusCode, IResult)> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<(HttpStatusCode, IDataResult<List<ProductVariant>>)> GetVariantsAsync(int productId, CancellationToken cancellationToken = default);
    Task<(HttpStatusCode, IResult)> AddVariantAsync(ProductVariant variant, CancellationToken cancellationToken = default);
    Task<(HttpStatusCode, IResult)> UpdateStockAsync(int variantId, int stock, CancellationToken cancellationToken = default);
    Task<(HttpStatusCode, IResult)> DeleteVariantAsync(int variantId, CancellationToken cancellationToken = default);

    Task<(HttpStatusCode, IDataResult<List<ProductImage>>)> GetImagesAsync(int productId, CancellationToken cancellationToken = default);
    Task<(HttpStatusCode, IResult)> AddImageAsync(ProductImage image, CancellationToken cancellationToken = default);
    Task<(HttpStatusCode, IResult)> DeleteImageAsync(int imageId, CancellationToken cancellationToken = default);
}
