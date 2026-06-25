using Microsoft.EntityFrameworkCore;
using System.IO;

namespace BankDeposits.Models;

public class AppDbContext : DbContext
{
    public DbSet<Client> Clients { get; set; }
    public DbSet<Vklad> Vklads { get; set; }
    public DbSet<Account> Accounts { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "bankdeposits.db");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasOne(a => a.Client)
                  .WithMany(c => c.Accounts)
                  .HasForeignKey(a => a.ClientId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.Vklad)
                  .WithMany(v => v.Accounts)
                  .HasForeignKey(a => a.VkladId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasCheckConstraint("CK_Account_Amount", "amount > 0");
            entity.HasCheckConstraint("CK_Account_CloseDate", "close_date IS NULL OR close_date >= open_date");
        });

        modelBuilder.Entity<Vklad>(entity =>
        {
            entity.HasCheckConstraint("CK_Vklad_TermMonths", "term_months > 0");
            entity.HasCheckConstraint("CK_Vklad_Rate", "rate >= 0");
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasIndex(c => c.Passport).IsUnique();
        });
    }
}