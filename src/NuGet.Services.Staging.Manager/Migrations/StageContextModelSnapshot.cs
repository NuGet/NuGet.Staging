// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using NuGet.Services.Staging.Database.Models;

namespace NuGet.Services.Staging.Manager.Migrations
{
    [DbContext(typeof(StageContext))]
    partial class StageContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.0.0-rc2-20828")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("NuGet.Services.Staging.Stage", b =>
                {
                    b.Property<int>("Key")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("CreationDate");

                    b.Property<string>("DisplayName")
                        .IsRequired();

                    b.Property<DateTime>("ExpirationDate");

                    b.Property<string>("Id")
                        .IsRequired()
                        .HasAnnotation("MaxLength", 32);

                    b.Property<int>("Status");

                    b.HasKey("Key");

                    b.HasIndex("Id");

                    b.ToTable("Stages");
                });

            modelBuilder.Entity("NuGet.Services.Staging.StageCommit", b =>
                {
                    b.Property<int>("Key")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("LastProgressUpdate");

                    b.Property<string>("Progress");

                    b.Property<DateTime>("RequestTime");

                    b.Property<int>("StageKey");

                    b.Property<int>("Status");

                    b.Property<string>("TrackId")
                        .IsRequired()
                        .HasAnnotation("MaxLength", 32);

                    b.HasKey("Key");

                    b.HasIndex("StageKey");

                    b.ToTable("StageCommit");
                });

            modelBuilder.Entity("NuGet.Services.Staging.StagedPackage", b =>
                {
                    b.Property<int>("Key")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Id")
                        .IsRequired()
                        .HasAnnotation("MaxLength", 128);

                    b.Property<string>("NormalizedVersion")
                        .IsRequired()
                        .HasAnnotation("MaxLength", 64);

                    b.Property<string>("NupkgUrl")
                        .IsRequired();

                    b.Property<string>("NuspecUrl")
                        .IsRequired();

                    b.Property<DateTime>("Published");

                    b.Property<int>("StageKey");

                    b.Property<int>("UserKey");

                    b.Property<string>("Version")
                        .IsRequired()
                        .HasAnnotation("MaxLength", 64);

                    b.HasKey("Key");

                    b.HasIndex("StageKey");

                    b.ToTable("StagedPackage");
                });

            modelBuilder.Entity("NuGet.Services.Staging.StageMembership", b =>
                {
                    b.Property<int>("Key")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("MembershipType");

                    b.Property<int>("StageKey");

                    b.Property<int>("UserKey");

                    b.HasKey("Key");

                    b.HasIndex("StageKey");

                    b.HasIndex("UserKey");

                    b.ToTable("StageMemberships");
                });

            modelBuilder.Entity("NuGet.Services.Staging.StageCommit", b =>
                {
                    b.HasOne("NuGet.Services.Staging.Stage")
                        .WithMany()
                        .HasForeignKey("StageKey")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NuGet.Services.Staging.StagedPackage", b =>
                {
                    b.HasOne("NuGet.Services.Staging.Stage")
                        .WithMany()
                        .HasForeignKey("StageKey")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NuGet.Services.Staging.StageMembership", b =>
                {
                    b.HasOne("NuGet.Services.Staging.Stage")
                        .WithMany()
                        .HasForeignKey("StageKey")
                        .OnDelete(DeleteBehavior.Cascade);
                });
        }
    }
}
