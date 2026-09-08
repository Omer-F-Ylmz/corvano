using Corvano.Core.DataAccess.EntityFramework;
using Corvano.DataAccess.Abstract;
using Corvano.DataAccess.Concrete.EntityFramework.Contexts;
using Corvano.Entities.Concrete;

namespace Corvano.DataAccess.Concrete.EntityFramework;

public class EfAdminUserDal : EfEntityRepositoryBase<AdminUser, CorvanoContext>, IAdminUserDal
{
    public EfAdminUserDal(CorvanoContext context) : base(context)
    {
    }
}
