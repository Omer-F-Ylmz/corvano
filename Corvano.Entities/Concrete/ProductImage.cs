using Corvano.Core.Entities;

namespace Corvano.Entities.Concrete;

public class ProductImage : IEntity
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Alt { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
