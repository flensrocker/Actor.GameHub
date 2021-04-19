module Actor.GameHub.DesignTimeServices

open EntityFrameworkCore.FSharp
open Microsoft.Extensions.DependencyInjection
open Microsoft.EntityFrameworkCore.Design

open Actor.GameHub
open Actor.GameHub.Identity.EntityFrameworkCore.IdentityDbContext

type IdentityDesignTimeDbContextFactory() =
    interface IDesignTimeDbContextFactory<IdentityDbContext> with
        member __.CreateDbContext(args: string []) =
            new IdentityDbContext(Configuration.dbOptions)

type DesignTimeServices() =
    interface IDesignTimeServices with
        member __.ConfigureDesignTimeServices(serviceCollection: IServiceCollection) =
            let scaffoldOptions =
                ScaffoldOptions(
                    ScaffoldTypesAs = ScaffoldTypesAs.RecordType,
                    ScaffoldNullableColumnsAs = ScaffoldNullableColumnsAs.OptionTypes
                )

            let fSharpServices =
                EFCoreFSharpServices.WithScaffoldOptions scaffoldOptions

            fSharpServices.ConfigureDesignTimeServices serviceCollection
            ()
