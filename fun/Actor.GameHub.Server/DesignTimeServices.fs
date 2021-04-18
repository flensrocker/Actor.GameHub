module Actor.GameHub.Identity.EntityFrameworkCore.DesignTimeServices

open System.Reflection
open Microsoft.Extensions.DependencyInjection
open Microsoft.EntityFrameworkCore.Design
open Microsoft.EntityFrameworkCore
open EntityFrameworkCore.FSharp
open Actor.GameHub.Identity.EntityFrameworkCore.IdentityDbContext

let connectionString =
    "Server=(localdb)\\MSSQLLocalDB;Database=ActorGameHub;Trusted_Connection=True;MultipleActiveResultSets=true"

type IdentityDesignTimeDbContextFactory() =
    interface IDesignTimeDbContextFactory<IdentityDbContext> with
        member __.CreateDbContext(args: string []) =
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

            new IdentityDbContext(dbOptions)

type DesignTimeServices() =
    interface IDesignTimeServices with
        member __.ConfigureDesignTimeServices(serviceCollection: IServiceCollection) =
            let scaffoldOptions =
                ScaffoldOptions (
                    ScaffoldTypesAs = ScaffoldTypesAs.RecordType,
                    ScaffoldNullableColumnsAs = ScaffoldNullableColumnsAs.OptionTypes)

            let fSharpServices = EFCoreFSharpServices.WithScaffoldOptions scaffoldOptions
            fSharpServices.ConfigureDesignTimeServices serviceCollection
            ()
