using Corvano.DataAccess.Concrete.EntityFramework.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Corvano.Tests;

public static class TestDb
{
    private const string LocalDbConnection =
        @"Server=(localdb)\MSSQLLocalDB;Database=CorvanoTest;Trusted_Connection=True;TrustServerCertificate=True";

    public static string ConnectionString { get; } =
        Environment.GetEnvironmentVariable("ConnectionStrings__Default") ?? LocalDbConnection;

    public static DbContextOptions<CorvanoContext> Options => new DbContextOptionsBuilder<CorvanoContext>()
        .UseSqlServer(ConnectionString)
        .Options;

    public static CorvanoContext NewContext() => new(Options);

    public static async Task ResetAsync()
    {
        await using var context = NewContext();
        await context.Database.MigrateAsync();
        await context.ProductImages.ExecuteDeleteAsync();
        await context.ProductVariants.ExecuteDeleteAsync();
        await context.Products.ExecuteDeleteAsync();
        await context.Categories.ExecuteDeleteAsync();
        await context.AdminUsers.ExecuteDeleteAsync();
    }
}
