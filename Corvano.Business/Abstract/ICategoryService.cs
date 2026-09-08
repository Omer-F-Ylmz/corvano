using System.Net;
using Corvano.Core.Utilities.Results;
using Corvano.Entities.Concrete;

namespace Corvano.Business.Abstract;

public interface ICategoryService
{
    Task<(HttpStatusCode, IDataResult<List<Category>>)> GetAllAsync(CancellationToken cancellationToken = default);
    Task<(HttpStatusCode, IDataResult<Category>)> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(HttpStatusCode, IResult)> AddAsync(Category category, CancellationToken cancellationToken = default);
    Task<(HttpStatusCode, IResult)> UpdateAsync(Category category, CancellationToken cancellationToken = default);
    Task<(HttpStatusCode, IResult)> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
