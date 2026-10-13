using Microsoft.EntityFrameworkCore;
using TurkiyeSimulasyonu.Api.Entities;

namespace TurkiyeSimulasyonu.Api.Data;

public sealed class AppDbContext : DbContext
{
    // Ayarlar (UseSqlServer, adres) Program.cs'teki AddDbContext'ten buraya, buradan EF'e gelir
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Her tabloya açılan kapı
    public DbSet<Category> Categories { get; set; }
    public DbSet<Item> Items { get; set; }
    public DbSet<Comment> Comments { get; set; }

    // Veritabanı kuralları tek yerde: uzunluk, tekillik, ilişki, silme davranışı
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(category =>
        {
            category.Property(c => c.Slug).HasMaxLength(100);
            category.HasIndex(c => c.Slug).IsUnique();
            category.Property(c => c.Name).HasMaxLength(100);
            category.Property(c => c.Emoji).HasMaxLength(32);
        });

        modelBuilder.Entity<Item>(item =>
        {
            // SEN: Slug (100 + tekil), Name (100), Emoji (32), Summary (500)
            item.Property(i => i.Slug).HasMaxLength(100);
            item.HasIndex(i => i.Slug).IsUnique();
            item.Property(i => i.Name).HasMaxLength(100);
            item.Property(i => i.Emoji).HasMaxLength(32);
            item.Property(i => i.Summary).HasMaxLength(500);

            // Her öğe bir kategoriye ait; öğesi olan kategori silinemez
            item.HasOne(i => i.Category)
                .WithMany(c => c.Items)
                .HasForeignKey(i => i.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Comment>(comment =>
        {
            // 30 ve 500 sözleşmeden (sunucu doğrulaması Gün 55)
            comment.Property(co => co.Author).HasMaxLength(30);
            comment.Property(co => co.Body).HasMaxLength(500);


            // Yorum, öğesi olmadan anlamsız; öğe silinirse yorumları da silinir
            comment.HasOne(co => co.Item)
                   .WithMany(i => i.Comments)
                   .HasForeignKey(co => co.ItemId)
                   .OnDelete(DeleteBehavior.Cascade);
        });
    }
}