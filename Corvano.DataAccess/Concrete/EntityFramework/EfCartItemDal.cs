using Corvano.Core.DataAccess.EntityFramework;
using Corvano.DataAccess.Abstract;
using Corvano.DataAccess.Concrete.EntityFramework.Contexts;
using Corvano.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace Corvano.DataAccess.Concrete.EntityFramework;

public class EfCartItemDal : EfEntityRepositoryBase<CartItem, CorvanoContext>, ICartItemDal
{
    public EfCartItemDal(CorvanoContext context) : base(context)
    {
    }

    public void Detach(CartItem item) => Context.Entry(item).State = EntityState.Detached;
}
