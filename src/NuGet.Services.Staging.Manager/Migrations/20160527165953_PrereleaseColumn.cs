using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NuGet.Services.Staging.Manager.Migrations
{
    public partial class PrereleaseColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Version",
                table: "PackagesMetadata",
                nullable: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrerelease",
                table: "PackagesMetadata",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_PackagesMetadata_IsPrerelease",
                table: "PackagesMetadata",
                column: "IsPrerelease");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PackagesMetadata_IsPrerelease",
                table: "PackagesMetadata");

            migrationBuilder.DropColumn(
                name: "IsPrerelease",
                table: "PackagesMetadata");

            migrationBuilder.AlterColumn<string>(
                name: "Version",
                table: "PackagesMetadata",
                nullable: true);
        }
    }
}
