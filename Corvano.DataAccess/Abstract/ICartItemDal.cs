using Corvano.Core.DataAccess;
using Corvano.Entities.Concrete;

namespace Corvano.DataAccess.Abstract;

public interface ICartItemDal : IEntityRepository<CartItem>
{
    /// <summary>Kaydı başarısız olan satırı bağlamdan çıkarır; yeniden deneme onu tekrar eklemeye çalışmaz.</summary>
    void Detach(CartItem item);
}
