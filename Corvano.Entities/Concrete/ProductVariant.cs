using Corvano.Core.Entities;

namespace Corvano.Entities.Concrete;

public class ProductVariant : IEntity
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Size { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int Stock { get; set; }
    public decimal? PriceOverride { get; set; }
}
