module Actor.GameHub.Identity.EntityFrameworkCore.IdentityDbContext

open System
open Microsoft.EntityFrameworkCore
open Actor.GameHub.Identity.EntityFrameworkCore.Entities

type IdentityDbContext(dbOptions: DbContextOptions<IdentityDbContext>) =
    inherit DbContext(dbOptions)

    [<DefaultValue>]
    val mutable private _user: DbSet<UserEntity>

    member this.User
        with get () = this._user
        and set v = this._user <- v

    override this.OnModelCreating modelBuilder =
        modelBuilder.HasDefaultSchema "Identity" |> ignore

        let userBuilder = modelBuilder.Entity<UserEntity>()
        userBuilder.ToTable "User" |> ignore

        userBuilder.HasKey(nameof (Unchecked.defaultof<UserEntity>.Id))
        |> ignore

        userBuilder
            .Property(nameof (Unchecked.defaultof<UserEntity>.Username))
            .HasMaxLength(100)
            .IsRequired()
        |> ignore

        userBuilder
            .HasIndex(nameof (Unchecked.defaultof<UserEntity>.Username))
            .IsUnique()
        |> ignore

        userBuilder.HasData(
            { Id = Guid.NewGuid()
              Username = "lars" },
            { Id = Guid.NewGuid()
              Username = "merten" },
            { Id = Guid.NewGuid()
              Username = "sam" },
            { Id = Guid.NewGuid()
              Username = "uli" }
        )
        |> ignore
