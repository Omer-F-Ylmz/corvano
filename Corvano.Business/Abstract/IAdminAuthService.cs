using System.Net;
using Corvano.Core.Utilities.Results;
using Corvano.Entities.Concrete;

namespace Corvano.Business.Abstract;

public interface IAdminAuthService
{
    Task<(HttpStatusCode, IDataResult<AdminUser>)> SignInAsync(string email, string password, CancellationToken cancellationToken = default);

    /// <summary>İlk yöneticiyi oluşturur; e-posta zaten varsa dokunmaz.</summary>
    Task<(HttpStatusCode, IResult)> EnsureSeedAsync(string email, string password, CancellationToken cancellationToken = default);
}
