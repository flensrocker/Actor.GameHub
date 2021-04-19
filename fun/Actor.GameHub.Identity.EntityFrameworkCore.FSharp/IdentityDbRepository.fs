module Actor.GameHub.Identity.EntityFrameworkCore.IdentityDbRepository

open Microsoft.EntityFrameworkCore

open Actor.GameHub.Identity.Abstractions
open Actor.GameHub.Identity.EntityFrameworkCore.IdentityDbContext

type IdentityDbRepository(dbOptions: DbContextOptions<IdentityDbContext>) =
    interface IIdentityRepository with
        member this.FindUserByUsernameForAuth username =
            let dbContext = new IdentityDbContext(dbOptions)

            let userQuery =
                query {
                    for user in dbContext.User do
                        where (user.Username = username)

                        select
                            { UserId = user.Id
                              Username = user.Username }
                }

            userQuery |> Seq.tryHead

let newIdentityDbRepository dbOptions = new IdentityDbRepository(dbOptions)
