using System;
using System.Collections.Generic;
using Microsoft.Data.Entity.Migrations;
using Microsoft.Data.Entity.Metadata;

namespace NuGet.Services.Staging.Manager.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Stage",
                columns: table => new
                {
                    Key = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CreationDate = table.Column<DateTime>(nullable: false),
                    DisplayName = table.Column<string>(nullable: false),
                    ExpirationDate = table.Column<DateTime>(nullable: false),
                    Id = table.Column<string>(nullable: false),
                    Status = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stage", x => x.Key);
                });
            migrationBuilder.CreateTable(
                name: "StageCommit",
                columns: table => new
                {
                    Key = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ErrorDetails = table.Column<string>(nullable: true),
                    LastProgressUpdate = table.Column<DateTime>(nullable: false),
                    Progress = table.Column<string>(nullable: true),
                    RequestTime = table.Column<DateTime>(nullable: false),
                    StageKey = table.Column<int>(nullable: false),
                    Status = table.Column<int>(nullable: false),
                    TrackId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StageCommit", x => x.Key);
                    table.ForeignKey(
                        name: "FK_StageCommit_Stage_StageKey",
                        column: x => x.StageKey,
                        principalTable: "Stage",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                });
            migrationBuilder.CreateTable(
                name: "StagedPackage",
                columns: table => new
                {
                    Key = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Id = table.Column<string>(nullable: false),
                    NormalizedVersion = table.Column<string>(nullable: false),
                    NupkgUrl = table.Column<string>(nullable: false),
                    Published = table.Column<DateTime>(nullable: false),
                    StageKey = table.Column<int>(nullable: false),
                    UserKey = table.Column<int>(nullable: false),
                    Version = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StagedPackage", x => x.Key);
                    table.ForeignKey(
                        name: "FK_StagedPackage_Stage_StageKey",
                        column: x => x.StageKey,
                        principalTable: "Stage",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                });
            migrationBuilder.CreateTable(
                name: "StageMembership",
                columns: table => new
                {
                    Key = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    MembershipType = table.Column<int>(nullable: false),
                    StageKey = table.Column<int>(nullable: false),
                    UserKey = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StageMembership", x => x.Key);
                    table.ForeignKey(
                        name: "FK_StageMembership_Stage_StageKey",
                        column: x => x.StageKey,
                        principalTable: "Stage",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                });
            migrationBuilder.CreateIndex(
                name: "IX_Stage_Id",
                table: "Stage",
                column: "Id");
            migrationBuilder.CreateIndex(
                name: "IX_StageCommit_StageKey",
                table: "StageCommit",
                column: "StageKey");
            migrationBuilder.CreateIndex(
                name: "IX_StagedPackage_StageKey",
                table: "StagedPackage",
                column: "StageKey");
            migrationBuilder.CreateIndex(
                name: "IX_StageMembership_StageKey",
                table: "StageMembership",
                column: "StageKey");
            migrationBuilder.CreateIndex(
                name: "IX_StageMembership_UserKey",
                table: "StageMembership",
                column: "UserKey");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("StageCommit");
            migrationBuilder.DropTable("StagedPackage");
            migrationBuilder.DropTable("StageMembership");
            migrationBuilder.DropTable("Stage");
        }
    }
}
