using Corvano.Core.DataAccess.EntityFramework;
using Corvano.DataAccess.Abstract;
using Corvano.DataAccess.Concrete.EntityFramework.Contexts;
using Corvano.Entities.Concrete;

namespace Corvano.DataAccess.Concrete.EntityFramework;

public class EfCartDal : EfEntityRepositoryBase<Cart, CorvanoContext>, ICartDal
{
    public EfCartDal(CorvanoContext context) : base(context)
    {
    }
}
