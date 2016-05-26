using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

namespace NuGet.Services.Staging.Manager.Migrations
{
    public partial class PackageMetadataTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PackagesMetadata",
                columns: table => new
                {
                    Key = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Authors = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    IconUrl = table.Column<string>(nullable: true),
                    Id = table.Column<string>(nullable: false),
                    LicenseUrl = table.Column<string>(nullable: true),
                    Owners = table.Column<string>(nullable: true),
                    ProjectUrl = table.Column<string>(nullable: true),
                    StageKey = table.Column<int>(nullable: false),
                    StagedPackageKey = table.Column<int>(nullable: false),
                    Summary = table.Column<string>(nullable: true),
                    Tags = table.Column<string>(nullable: true),
                    Title = table.Column<string>(nullable: true),
                    Version = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackagesMetadata", x => x.Key);
                    table.ForeignKey(
                        name: "FK_PackagesMetadata_Stages_StageKey",
                        column: x => x.StageKey,
                        principalTable: "Stages",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_PackagesMetadata_StagedPackage_StagedPackageKey",
                        column: x => x.StagedPackageKey,
                        principalTable: "StagedPackage",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PackagesMetadata_StageKey",
                table: "PackagesMetadata",
                column: "StageKey");

            migrationBuilder.CreateIndex(
                name: "IX_PackagesMetadata_StagedPackageKey",
                table: "PackagesMetadata",
                column: "StagedPackageKey");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PackagesMetadata");
        }
    }
}
