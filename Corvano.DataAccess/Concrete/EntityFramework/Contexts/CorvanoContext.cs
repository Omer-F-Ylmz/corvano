using Corvano.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace Corvano.DataAccess.Concrete.EntityFramework.Contexts;

public class CorvanoContext : DbContext
{
    public CorvanoContext(DbContextOptions<CorvanoContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("category");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnName("id");
            e.Property(c => c.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
            e.Property(c => c.Slug).HasColumnName("slug").HasMaxLength(140).IsRequired();
            e.Property(c => c.ParentId).HasColumnName("parent_id");
            e.Property(c => c.SortOrder).HasColumnName("sort_order");
            e.Property(c => c.IsActive).HasColumnName("is_active");
            e.HasIndex(c => c.Slug).IsUnique().HasDatabaseName("ux_category_slug");
            e.HasOne<Category>().WithMany().HasForeignKey(c => c.ParentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("product");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            e.Property(p => p.Slug).HasColumnName("slug").HasMaxLength(200).IsRequired();
            e.Property(p => p.Description).HasColumnName("description").HasColumnType("nvarchar(max)").IsRequired();
            e.Property(p => p.CategoryId).HasColumnName("category_id");
            e.Property(p => p.Price).HasColumnName("price").HasPrecision(18, 2);
            e.Property(p => p.IsActive).HasColumnName("is_active");
            e.Property(p => p.IsFeatured).HasColumnName("is_featured");
            e.Property(p => p.CreatedAt).HasColumnName("created_at");
            e.Property(p => p.UpdatedAt).HasColumnName("updated_at");
            e.HasIndex(p => p.Slug).IsUnique().HasDatabaseName("ux_product_slug");
            e.HasOne<Category>().WithMany().HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductVariant>(e =>
        {
            e.ToTable("product_variant", t => t.HasCheckConstraint("ck_product_variant_stock", "[stock] >= 0"));
            e.HasKey(v => v.Id);
            e.Property(v => v.Id).HasColumnName("id");
            e.Property(v => v.ProductId).HasColumnName("product_id");
            e.Property(v => v.Size).HasColumnName("size").HasMaxLength(8).IsRequired();
            e.Property(v => v.Color).HasColumnName("color").HasMaxLength(60).IsRequired();
            e.Property(v => v.Sku).HasColumnName("sku").HasMaxLength(60).IsRequired();
            e.Property(v => v.Stock).HasColumnName("stock");
            e.Property(v => v.PriceOverride).HasColumnName("price_override").HasPrecision(18, 2);
            e.HasIndex(v => v.Sku).IsUnique().HasDatabaseName("ux_product_variant_sku");
            e.HasOne<Product>().WithMany().HasForeignKey(v => v.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProductImage>(e =>
        {
            e.ToTable("product_image");
            e.HasKey(i => i.Id);
            e.Property(i => i.Id).HasColumnName("id");
            e.Property(i => i.ProductId).HasColumnName("product_id");
            e.Property(i => i.Url).HasColumnName("url").HasMaxLength(500).IsRequired();
            e.Property(i => i.Alt).HasColumnName("alt").HasMaxLength(200).IsRequired();
            e.Property(i => i.SortOrder).HasColumnName("sort_order");
            e.HasOne<Product>().WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AdminUser>(e =>
        {
            e.ToTable("admin_user");
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).HasColumnName("id");
            e.Property(a => a.Email).HasColumnName("email").HasMaxLength(160).IsRequired();
            e.Property(a => a.PasswordHash).HasColumnName("password_hash").HasMaxLength(400).IsRequired();
            e.Property(a => a.FailedAttempts).HasColumnName("failed_attempts");
            e.Property(a => a.LockedUntil).HasColumnName("locked_until");
            e.HasIndex(a => a.Email).IsUnique().HasDatabaseName("ux_admin_user_email");
        });

        modelBuilder.Entity<Cart>(e =>
        {
            e.ToTable("cart");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnName("id");
            e.Property(c => c.CartKey).HasColumnName("cart_key");
            e.Property(c => c.CustomerId).HasColumnName("customer_id");
            e.Property(c => c.CreatedAt).HasColumnName("created_at");
            e.Property(c => c.UpdatedAt).HasColumnName("updated_at");
            e.HasIndex(c => c.CartKey).IsUnique().HasDatabaseName("ux_cart_cart_key");
        });

        modelBuilder.Entity<CartItem>(e =>
        {
            e.ToTable("cart_item", t => t.HasCheckConstraint("ck_cart_item_quantity", "[quantity] >= 1"));
            e.HasKey(i => i.Id);
            e.Property(i => i.Id).HasColumnName("id");
            e.Property(i => i.CartId).HasColumnName("cart_id");
            e.Property(i => i.ProductVariantId).HasColumnName("product_variant_id");
            e.Property(i => i.Quantity).HasColumnName("quantity");
            e.Property(i => i.UnitPrice).HasColumnName("unit_price").HasPrecision(18, 2);
            e.Property(i => i.AddedAt).HasColumnName("added_at");
            e.HasIndex(i => new { i.CartId, i.ProductVariantId }).IsUnique().HasDatabaseName("ux_cart_item_cart_variant");
            e.HasOne<Cart>().WithMany().HasForeignKey(i => i.CartId).OnDelete(DeleteBehavior.Cascade);
            // varyant silinirse sepet satırı da gider: FK şartı silinmiş varyantın satırda kalmasına izin vermez
            e.HasOne<ProductVariant>().WithMany().HasForeignKey(i => i.ProductVariantId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
