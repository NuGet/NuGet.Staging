// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

namespace NuGet.Services.Staging.Manager.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Stages",
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
                    table.PrimaryKey("PK_Stages", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "StageCommit",
                columns: table => new
                {
                    Key = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
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
                        name: "FK_StageCommit_Stages_StageKey",
                        column: x => x.StageKey,
                        principalTable: "Stages",
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
                    NuspecUrl = table.Column<string>(nullable: false),
                    Published = table.Column<DateTime>(nullable: false),
                    StageKey = table.Column<int>(nullable: false),
                    UserKey = table.Column<int>(nullable: false),
                    Version = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StagedPackage", x => x.Key);
                    table.ForeignKey(
                        name: "FK_StagedPackage_Stages_StageKey",
                        column: x => x.StageKey,
                        principalTable: "Stages",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StageMemberships",
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
                    table.PrimaryKey("PK_StageMemberships", x => x.Key);
                    table.ForeignKey(
                        name: "FK_StageMemberships_Stages_StageKey",
                        column: x => x.StageKey,
                        principalTable: "Stages",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Stages_Id",
                table: "Stages",
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
                name: "IX_StageMemberships_StageKey",
                table: "StageMemberships",
                column: "StageKey");

            migrationBuilder.CreateIndex(
                name: "IX_StageMemberships_UserKey",
                table: "StageMemberships",
                column: "UserKey");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StageCommit");

            migrationBuilder.DropTable(
                name: "StagedPackage");

            migrationBuilder.DropTable(
                name: "StageMemberships");

            migrationBuilder.DropTable(
                name: "Stages");
        }
    }
}
