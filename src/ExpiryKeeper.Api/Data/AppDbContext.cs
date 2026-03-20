using Microsoft.EntityFrameworkCore;
using MedicineExpiration.Api.Models;

namespace MedicineExpiration.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Medicine> Medicines => Set<Medicine>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Medicine>(e =>
        {
            e.HasIndex(m => m.UserOid);
            e.Property(m => m.Name).HasMaxLength(200);
            e.Property(m => m.Barcode).HasMaxLength(100);
            e.Property(m => m.Category).HasMaxLength(100);
        });

        modelBuilder.Entity<PushSubscription>(e =>
        {
            e.HasIndex(p => p.UserOid);
            e.Property(p => p.Provider).HasConversion<string>();
        });

        modelBuilder.Entity<NotificationLog>(e =>
        {
            e.HasOne(n => n.Medicine)
             .WithMany(m => m.NotificationLogs)
             .HasForeignKey(n => n.MedicineId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
