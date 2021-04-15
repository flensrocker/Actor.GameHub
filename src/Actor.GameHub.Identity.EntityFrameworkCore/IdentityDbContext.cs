using System;
using Microsoft.EntityFrameworkCore;

namespace Actor.GameHub.Identity.EntityFrameworkCore
{
  public class IdentityDbContext : DbContext
  {
    public DbSet<UserEntity> User { get; init; } = null!;

    public IdentityDbContext(DbContextOptions<IdentityDbContext> dbOptions)
      : base(dbOptions)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.HasDefaultSchema("Identity");
      
      var userBuilder = modelBuilder.Entity<UserEntity>();
      userBuilder.ToTable("User");
      userBuilder
        .Property(u => u.Username)
        .HasMaxLength(100)
        .IsRequired();
      userBuilder
        .HasIndex(u => u.Username)
        .IsUnique();

      userBuilder.HasData(new UserEntity
      {
        Id = Guid.NewGuid(),
        Username = "lars",
      }, new UserEntity
      {
        Id = Guid.NewGuid(),
        Username = "merten",
      }, new UserEntity
      {
        Id = Guid.NewGuid(),
        Username = "sam",
      }, new UserEntity
      {
        Id = Guid.NewGuid(),
        Username = "uli",
      });

      base.OnModelCreating(modelBuilder);
    }
  }
}
