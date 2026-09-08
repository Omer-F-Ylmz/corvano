using Corvano.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace Corvano.DataAccess.Concrete.EntityFramework.Contexts;

public class CorvanoContext : DbContext
{
    public CorvanoContext(DbContextOptions<CorvanoContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("product");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            e.Property(p => p.Slug).HasColumnName("slug").HasMaxLength(200).IsRequired();
            e.Property(p => p.Price).HasColumnName("price").HasPrecision(18, 2);
            e.Property(p => p.IsActive).HasColumnName("is_active");
            e.Property(p => p.CreatedAt).HasColumnName("created_at");
            e.HasIndex(p => p.Slug).IsUnique().HasDatabaseName("ux_product_slug");
        });
    }
}
