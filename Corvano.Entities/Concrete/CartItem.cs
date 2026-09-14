using Corvano.Core.Entities;

namespace Corvano.Entities.Concrete;

public class CartItem : IEntity
{
    public int Id { get; set; }
    public int CartId { get; set; }
    public int ProductVariantId { get; set; }
    public int Quantity { get; set; }

    /// <summary>Ekleme anındaki birim fiyat (varyant fiyat farkı varsa o).</summary>
    public decimal UnitPrice { get; set; }

    public DateTime AddedAt { get; set; }
}
