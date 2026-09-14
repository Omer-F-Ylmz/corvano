using System.Net;
using Corvano.Core.Utilities.Results;

namespace Corvano.Business.Abstract;

public interface ICatalogSeedService
{
    /// <summary>Geliştirme kataloğunu yazar; eksik olanı ekler, var olana dokunmaz.</summary>
    Task<(HttpStatusCode, IResult)> EnsureSeedAsync(CancellationToken cancellationToken = default);
}
