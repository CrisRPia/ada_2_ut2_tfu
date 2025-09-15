using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace bitwardenclone.src.models;

public class User
{
    public Guid Id { get; set; }

    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    public required string MasterPasswordHash { get; set; }
    public Vault? Vault { get; set; }
}

public class Vault
{
    public Guid Id { get; set; }

    [Required]
    public required string EncryptedData { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
}

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Vault> Vaults { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder
            .Entity<User>()
            .HasOne(u => u.Vault)
            .WithOne(v => v.User)
            .HasForeignKey<Vault>(v => v.UserId);

        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
    }
}
