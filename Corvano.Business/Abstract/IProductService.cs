using System.Net;
using Corvano.Core.Utilities.Results;
using Corvano.Entities.Concrete;

namespace Corvano.Business.Abstract;

public interface IProductService
{
    Task<(HttpStatusCode, IDataResult<List<Product>>)> GetAllAsync(CancellationToken cancellationToken = default);
    Task<(HttpStatusCode, IResult)> AddAsync(Product product, CancellationToken cancellationToken = default);
}
