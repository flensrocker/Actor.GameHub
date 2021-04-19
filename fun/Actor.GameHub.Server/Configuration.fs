module Actor.GameHub.Configuration

open System.Reflection
open Microsoft.EntityFrameworkCore
open Actor.GameHub.Identity.EntityFrameworkCore.IdentityDbContext

let connectionString =
    "Server=(localdb)\\MSSQLLocalDB;Database=ActorGameHub;Trusted_Connection=True;MultipleActiveResultSets=true"

let dbOptions =
    (new DbContextOptionsBuilder<IdentityDbContext>())
        .UseSqlServer(
        connectionString,
        fun sqlOptions ->
            sqlOptions.MigrationsAssembly(
                Assembly
                    .GetAssembly(
                        typedefof<IdentityDbContext>
                    )
                    .GetName()
                    .Name
            )
            |> ignore
    )
        .Options
