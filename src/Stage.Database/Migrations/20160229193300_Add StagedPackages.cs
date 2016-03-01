using System;
using System.Collections.Generic;
using Microsoft.Data.Entity.Migrations;
using Microsoft.Data.Entity.Metadata;

namespace Stage.Database.Migrations
{
    public partial class AddStagedPackages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_StageMember_Stage_StageKey", table: "StageMember");
            migrationBuilder.CreateTable(
                name: "StagedPackage",
                columns: table => new
                {
                    Key = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Id = table.Column<string>(nullable: false),
                    NormalizedVersion = table.Column<string>(nullable: false),
                    NupkgUrl = table.Column<string>(nullable: false),
                    PushDate = table.Column<DateTime>(nullable: false),
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
            migrationBuilder.CreateIndex(
                name: "IX_StagedPackage_StageKey",
                table: "StagedPackage",
                column: "StageKey");
            migrationBuilder.AddForeignKey(
                name: "FK_StageMember_Stage_StageKey",
                table: "StageMember",
                column: "StageKey",
                principalTable: "Stage",
                principalColumn: "Key",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_StageMember_Stage_StageKey", table: "StageMember");
            migrationBuilder.DropTable("StagedPackage");
            migrationBuilder.AddForeignKey(
                name: "FK_StageMember_Stage_StageKey",
                table: "StageMember",
                column: "StageKey",
                principalTable: "Stage",
                principalColumn: "Key",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
