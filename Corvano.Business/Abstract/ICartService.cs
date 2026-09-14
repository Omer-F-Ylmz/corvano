using System.Net;
using Corvano.Core.Utilities.Results;
using Corvano.Entities.Dtos;

namespace Corvano.Business.Abstract;

/// <summary>Misafir sepeti; stok yalnız kontrol edilir, düşülmez (D5 siparişte düşer).</summary>
public interface ICartService
{
    /// <summary>Sepeti okur; fiyatı değişen satırların birim fiyatını günceller ve işaretler.</summary>
    Task<(HttpStatusCode, IDataResult<CartDto>)> GetCartAsync(Guid? cartKey, CancellationToken cancellationToken = default);

    Task<int> GetItemCountAsync(Guid? cartKey, CancellationToken cancellationToken = default);

    /// <summary>Sepet yoksa oluşturur. Aynı varyant tekrar eklenirse adet toplanır; stok aşılırsa stoğa sabitlenir.</summary>
    Task<(HttpStatusCode, IDataResult<CartAddResultDto>)> AddAsync(Guid? cartKey, int variantId, int quantity, CancellationToken cancellationToken = default);

    /// <summary>Adet 0 satırı siler; stoktan fazlası 409.</summary>
    Task<(HttpStatusCode, IResult)> UpdateQuantityAsync(Guid? cartKey, int itemId, int quantity, CancellationToken cancellationToken = default);

    Task<(HttpStatusCode, IResult)> RemoveAsync(Guid? cartKey, int itemId, CancellationToken cancellationToken = default);
}
