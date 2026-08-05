using DevPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevPulse.Infrastructure.Persistence;

public sealed class DevPulseDbContext : DbContext
{
    public DevPulseDbContext(DbContextOptions<DevPulseDbContext> options) : base(options)
    {
    }

    public DbSet<ClickUpAccount> ClickUpAccounts => Set<ClickUpAccount>();

    public DbSet<Developer> Developers => Set<Developer>();

    public DbSet<DeveloperClickUpMapping> DeveloperClickUpMappings => Set<DeveloperClickUpMapping>();

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

        modelBuilder.Entity<Developer>(entity =>
        {
            entity.ToTable("Developers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(320);
            entity.HasIndex(x => x.Email);
        });

        modelBuilder.Entity<DeveloperClickUpMapping>(entity =>
        {
            entity.ToTable("DeveloperClickUpMappings");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.DeveloperId, x.ClickUpAccountId }).IsUnique();
            entity.HasIndex(x => new { x.ClickUpAccountId, x.ClickUpUserId }).IsUnique();

            entity.HasOne(x => x.Developer)
                .WithMany(x => x.ClickUpMappings)
                .HasForeignKey(x => x.DeveloperId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.ClickUpAccount)
                .WithMany()
                .HasForeignKey(x => x.ClickUpAccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
