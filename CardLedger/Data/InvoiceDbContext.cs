using CardLedger.Models;
using Microsoft.EntityFrameworkCore;

namespace CardLedger.Data
{
    public class InvoiceDbContext : DbContext
    {
        public InvoiceDbContext(DbContextOptions<InvoiceDbContext> options) : base(options)
        {
        }

        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Índices para melhor performance
            modelBuilder.Entity<Transaction>()
                .HasIndex(t => new { t.Year, t.Month });

            modelBuilder.Entity<Transaction>()
                .HasIndex(t => t.Category);

            modelBuilder.Entity<Transaction>()
                .HasIndex(t => t.Date);
        }
    }
}
