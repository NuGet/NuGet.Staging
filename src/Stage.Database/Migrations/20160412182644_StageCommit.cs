using System;
using System.Collections.Generic;
using Microsoft.Data.Entity.Migrations;
using Microsoft.Data.Entity.Metadata;

namespace Stage.Manager.Migrations
{
    public partial class StageCommit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_StagedPackage_Stage_StageKey", table: "StagedPackage");
            migrationBuilder.DropForeignKey(name: "FK_StageMember_Stage_StageKey", table: "StageMember");
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
            migrationBuilder.CreateIndex(
                name: "IX_StageCommit_StageKey",
                table: "StageCommit",
                column: "StageKey");
            migrationBuilder.AddForeignKey(
                name: "FK_StagedPackage_Stage_StageKey",
                table: "StagedPackage",
                column: "StageKey",
                principalTable: "Stage",
                principalColumn: "Key",
                onDelete: ReferentialAction.Cascade);
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
            migrationBuilder.DropForeignKey(name: "FK_StagedPackage_Stage_StageKey", table: "StagedPackage");
            migrationBuilder.DropForeignKey(name: "FK_StageMember_Stage_StageKey", table: "StageMember");
            migrationBuilder.DropTable("StageCommit");
            migrationBuilder.AddForeignKey(
                name: "FK_StagedPackage_Stage_StageKey",
                table: "StagedPackage",
                column: "StageKey",
                principalTable: "Stage",
                principalColumn: "Key",
                onDelete: ReferentialAction.Restrict);
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
