using System.Linq.Expressions;
using Corvano.Core.Entities;

namespace Corvano.Core.DataAccess;

public interface IEntityRepository<T> where T : class, IEntity, new()
{
    Task<T?> GetAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default);
    Task<List<T>> GetListAsync(Expression<Func<T, bool>>? filter = null, CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Delete(T entity);
}
