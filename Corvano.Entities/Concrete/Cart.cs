using Corvano.Core.Entities;

namespace Corvano.Entities.Concrete;

public class Cart : IEntity
{
    public int Id { get; set; }
    public Guid CartKey { get; set; }
    public int? CustomerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
