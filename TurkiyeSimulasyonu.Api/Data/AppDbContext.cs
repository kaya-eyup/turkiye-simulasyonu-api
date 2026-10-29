using Microsoft.EntityFrameworkCore;
using TurkiyeSimulasyonu.Api.Entities;
using TurkiyeSimulasyonu.Api.Search;
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
    public DbSet<RebirthVote> RebirthVotes { get; set; }

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
            // Slug (100 + tekil), Name (100), Emoji (32), Summary (500)
            item.Property(i => i.Slug).HasMaxLength(100);
            item.HasIndex(i => i.Slug).IsUnique();
            item.Property(i => i.Name).HasMaxLength(100);
            item.Property(i => i.Emoji).HasMaxLength(32);
            item.Property(i => i.Summary).HasMaxLength(500);
            // 100 (Name) + 1 + 500 (Summary) = en fazla 601; index yok (LIKE '%…%' index kullanamaz)
            item.Property(i => i.SearchText).HasMaxLength(700);

            // Her öğe bir kategoriye ait; öğesi olan kategori silinemez
            item.HasOne(i => i.Category)
                .WithMany(c => c.Items)
                .HasForeignKey(i => i.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Comment>(comment =>
        {
            // 30 ve 500 sözleşmeden 
            comment.Property(co => co.Author).HasMaxLength(30);
            comment.Property(co => co.Body).HasMaxLength(500);

            // Yorum, öğesi olmadan anlamsız; öğe silinirse yorumları da silinir
            comment.HasOne(co => co.Item)
                   .WithMany(i => i.Comments)
                   .HasForeignKey(co => co.ItemId)
                   .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RebirthVote>(vote =>
        {
            // Tarayıcı başına tek oy: ikinci satırı kod değil bu index engeller (aynı anda gelen isteklerde de)
            vote.HasIndex(v => v.VoterId).IsUnique();

            // Hep 2 harf, hep ASCII → char(2): 2 bayt (nvarchar(2) 6 bayt tutardı)
            vote.Property(v => v.CountryCode).HasMaxLength(2).IsFixedLength().IsUnicode(false);

            // GROUP BY CountryCode bütün tabloyu değil bu dar index'i sırayla okur
            vote.HasIndex(v => v.CountryCode);
        });
    }

    // SearchText tek yerden dolar: DbSeeder, Importer ve API aynı kuralı kullanır, kimse unutamaz.
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        FillSearchText();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        FillSearchText();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void FillSearchText()
    {
        foreach (var entry in ChangeTracker.Entries<Item>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.SearchText =
                    SearchNormalizer.Normalize($"{entry.Entity.Name} {entry.Entity.Summary}");
            }
        }
    }
}