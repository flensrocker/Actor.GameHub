module Actor.GameHub.Identity.EntityFrameworkCore.UserEntity

open System
open Microsoft.EntityFrameworkCore
open Actor.GameHub.Identity.EntityFrameworkCore.IdentityDbConstants

type UserEntity = { Id: Guid; Username: string }

type UserEntityConfiguration() =
    interface IEntityTypeConfiguration<UserEntity> with

        member this.Configure userBuilder =
            userBuilder.ToTable(TableNameUser, DbSchemaName)
            |> ignore

            userBuilder.HasKey(nameof (Unchecked.defaultof<UserEntity>.Id))
            |> ignore

            userBuilder
                .Property(nameof (Unchecked.defaultof<UserEntity>.Username))
                .HasMaxLength(MaxLengthUsername)
                .IsRequired()
            |> ignore

            userBuilder
                .HasIndex(nameof (Unchecked.defaultof<UserEntity>.Username))
                .IsUnique()
            |> ignore

            ()
