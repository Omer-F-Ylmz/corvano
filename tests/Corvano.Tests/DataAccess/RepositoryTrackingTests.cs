using Corvano.DataAccess.Concrete.EntityFramework;
using Corvano.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace Corvano.Tests.DataAccess;

[Collection(DatabaseCollection.Name)]
public sealed class RepositoryTrackingTests : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await TestDb.ResetAsync();
        await using var context = TestDb.NewContext();
        await new EfCategoryDal(context).AddAsync(new Category { Name = "Gömlek", Slug = "gomlek", SortOrder = 1, IsActive = true });
        await new EfUnitOfWork(context).SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetAsync_reads_without_tracking_and_GetTrackedAsync_tracks()
    {
        await using var context = TestDb.NewContext();
        var dal = new EfCategoryDal(context);

        var readOnly = await dal.GetAsync(c => c.Slug == "gomlek");
        Assert.NotNull(readOnly);
        Assert.Equal(EntityState.Detached, context.Entry(readOnly!).State);

        var tracked = await dal.GetTrackedAsync(c => c.Slug == "gomlek");
        Assert.NotNull(tracked);
        Assert.Equal(EntityState.Unchanged, context.Entry(tracked!).State);
    }
}
