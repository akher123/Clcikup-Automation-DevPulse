using DevPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevPulse.Infrastructure.Persistence;

public sealed class DevPulseDbContext : DbContext
{
    public DevPulseDbContext(DbContextOptions<DevPulseDbContext> options) : base(options)
    {
    }

    public DbSet<ClickUpAccount> ClickUpAccounts => Set<ClickUpAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClickUpAccount>(entity =>
        {
            entity.ToTable("ClickUpAccounts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.WorkspaceId).HasMaxLength(50).IsRequired();
            entity.Property(x => x.EncryptedAccessToken).IsRequired();
            entity.Property(x => x.LastValidationMessage).HasMaxLength(500);
            entity.HasIndex(x => x.WorkspaceId).IsUnique();
        });
    }
}
