using CardLedger.Models;
using Microsoft.EntityFrameworkCore;

namespace CardLedger.Data;

public class InvoiceDbContext : DbContext
{
    public InvoiceDbContext(DbContextOptions<InvoiceDbContext> options) : base(options)
    {
    }

    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Category> Categories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>()
            .HasIndex(c => c.Name)
            .IsUnique();

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.CategoryEntity)
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índices para melhor performance
        modelBuilder.Entity<Transaction>()
            .HasIndex(t => new { t.Year, t.Month });

        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.InvoiceKey);

        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.CategoryId);

        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.Date);

        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Não Categorizado", CreatedAt = now, UpdatedAt = now },
            new Category { Id = 2, Name = "Alimentação", CreatedAt = now, UpdatedAt = now },
            new Category { Id = 3, Name = "Supermercado", CreatedAt = now, UpdatedAt = now },
            new Category { Id = 4, Name = "Transporte", CreatedAt = now, UpdatedAt = now },
            new Category { Id = 5, Name = "Carro", CreatedAt = now, UpdatedAt = now },
            new Category { Id = 6, Name = "Games", CreatedAt = now, UpdatedAt = now },
            new Category { Id = 7, Name = "Assinaturas & Contas", CreatedAt = now, UpdatedAt = now },
            new Category { Id = 8, Name = "Mayara", CreatedAt = now, UpdatedAt = now },
            new Category { Id = 9, Name = "Saúde", CreatedAt = now, UpdatedAt = now },
            new Category { Id = 10, Name = "Parcelado", CreatedAt = now, UpdatedAt = now },
            new Category { Id = 11, Name = "Compras Avulsas", CreatedAt = now, UpdatedAt = now },
            new Category { Id = 12, Name = "Terceiros", CreatedAt = now, UpdatedAt = now }
        );
    }
}
