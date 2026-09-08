using Corvano.Core.DataAccess.EntityFramework;
using Corvano.DataAccess.Abstract;
using Corvano.DataAccess.Concrete.EntityFramework.Contexts;
using Corvano.Entities.Concrete;

namespace Corvano.DataAccess.Concrete.EntityFramework;

public class EfProductVariantDal : EfEntityRepositoryBase<ProductVariant, CorvanoContext>, IProductVariantDal
{
    public EfProductVariantDal(CorvanoContext context) : base(context)
    {
    }
}
