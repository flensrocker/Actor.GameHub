using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Actor.GameHub.Identity.EntityFrameworkCore.Migrations
{
    public partial class IdentityInit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Identity");

            migrationBuilder.CreateTable(
                name: "User",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "Identity",
                table: "User",
                columns: new[] { "Id", "Username" },
                values: new object[,]
                {
                    { new Guid("3212fed2-6246-46a4-8959-79748024418e"), "lars" },
                    { new Guid("57e16b30-16fb-4203-8f22-7417e3ad91de"), "merten" },
                    { new Guid("aa493230-83b1-4cf6-b1d8-69aaf98e767d"), "sam" },
                    { new Guid("a1bc719c-2726-482e-bec9-7f826838d35f"), "uli" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_User_Username",
                schema: "Identity",
                table: "User",
                column: "Username",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "User",
                schema: "Identity");
        }
    }
}
