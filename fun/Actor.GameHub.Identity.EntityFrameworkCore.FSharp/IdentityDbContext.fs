module Actor.GameHub.Identity.EntityFrameworkCore.IdentityDbContext

open System
open Microsoft.EntityFrameworkCore
open Actor.GameHub.Identity.EntityFrameworkCore.UserEntity
open System.Reflection

type IdentityDbContext(dbOptions: DbContextOptions<IdentityDbContext>) =
    inherit DbContext(dbOptions)

    [<DefaultValue>]
    val mutable private _user: DbSet<UserEntity>

    member this.User
        with get () = this._user
        and set v = this._user <- v

    override this.OnModelCreating modelBuilder =
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly())
        |> ignore

        modelBuilder
            .Entity<UserEntity>()
            .HasData(
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
