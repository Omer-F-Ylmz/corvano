using Corvano.Core.Entities;

namespace Corvano.Entities.Concrete;

public class AdminUser : IEntity
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int FailedAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
}
