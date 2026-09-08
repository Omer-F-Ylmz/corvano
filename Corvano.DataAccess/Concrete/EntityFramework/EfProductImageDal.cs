using Corvano.Core.DataAccess.EntityFramework;
using Corvano.DataAccess.Abstract;
using Corvano.DataAccess.Concrete.EntityFramework.Contexts;
using Corvano.Entities.Concrete;

namespace Corvano.DataAccess.Concrete.EntityFramework;

public class EfProductImageDal : EfEntityRepositoryBase<ProductImage, CorvanoContext>, IProductImageDal
{
    public EfProductImageDal(CorvanoContext context) : base(context)
    {
    }
}
