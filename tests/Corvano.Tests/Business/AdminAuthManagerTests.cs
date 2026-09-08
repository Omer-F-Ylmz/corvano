using System.Net;
using Corvano.Business.Concrete;
using Corvano.DataAccess.Concrete.EntityFramework;
using Corvano.DataAccess.Concrete.EntityFramework.Contexts;

namespace Corvano.Tests.Business;

[Collection(DatabaseCollection.Name)]
public sealed class AdminAuthManagerTests : IAsyncLifetime
{
    private const string Email = "admin@corvano.com";
    private const string Password = "Corvano!Test1";

    public Task InitializeAsync() => TestDb.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static AdminAuthManager NewService(CorvanoContext context)
        => new(new EfAdminUserDal(context), new EfUnitOfWork(context));

    [Fact]
    public async Task Five_failed_attempts_lock_the_account_for_fifteen_minutes()
    {
        await using var context = TestDb.NewContext();
        var service = NewService(context);
        await service.EnsureSeedAsync(Email, Password);

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var (failedStatus, _) = await service.SignInAsync(Email, "yanlis-parola");
            Assert.Equal(HttpStatusCode.Unauthorized, failedStatus);
        }

        var (status, result) = await service.SignInAsync(Email, Password);

        Assert.Equal(HttpStatusCode.Locked, status);
        Assert.False(result.Success);

        var admin = await new EfAdminUserDal(context).GetAsync(a => a.Email == Email);
        Assert.NotNull(admin!.LockedUntil);
        Assert.InRange(admin.LockedUntil!.Value - DateTime.UtcNow, TimeSpan.FromMinutes(14), TimeSpan.FromMinutes(15));
    }

    [Fact]
    public async Task A_correct_password_clears_the_failed_attempt_counter()
    {
        await using var context = TestDb.NewContext();
        var service = NewService(context);
        await service.EnsureSeedAsync(Email, Password);
        await service.SignInAsync(Email, "yanlis-parola");
        await service.SignInAsync(Email, "yanlis-parola");

        var (status, result) = await service.SignInAsync(Email, Password);

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal(Email, result.Data!.Email);
        var admin = await new EfAdminUserDal(context).GetAsync(a => a.Email == Email);
        Assert.Equal(0, admin!.FailedAttempts);
        Assert.Null(admin.LockedUntil);
    }

    [Fact]
    public async Task An_unknown_email_is_unauthorized()
    {
        await using var context = TestDb.NewContext();
        var service = NewService(context);
        await service.EnsureSeedAsync(Email, Password);

        var (status, result) = await service.SignInAsync("yok@corvano.com", Password);

        Assert.Equal(HttpStatusCode.Unauthorized, status);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task Seeding_twice_keeps_a_single_admin_user()
    {
        await using var context = TestDb.NewContext();
        var service = NewService(context);

        await service.EnsureSeedAsync(Email, Password);
        await service.EnsureSeedAsync(Email, Password);

        var admins = await new EfAdminUserDal(context).GetListAsync();
        Assert.Single(admins);
    }
}
