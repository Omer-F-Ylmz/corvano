using Corvano.Core.DataAccess.EntityFramework;
using Corvano.DataAccess.Abstract;
using Corvano.DataAccess.Concrete.EntityFramework.Contexts;
using Corvano.Entities.Concrete;

namespace Corvano.DataAccess.Concrete.EntityFramework;

public class EfProductDal : EfEntityRepositoryBase<Product, CorvanoContext>, IProductDal
{
    public EfProductDal(CorvanoContext context) : base(context)
    {
    }
}
