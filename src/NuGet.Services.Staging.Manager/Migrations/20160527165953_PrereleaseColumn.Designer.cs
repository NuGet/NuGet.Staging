using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using NuGet.Services.Staging.Database.Models;

namespace NuGet.Services.Staging.Manager.Migrations
{
    [DbContext(typeof(StageContext))]
    [Migration("20160527165953_PrereleaseColumn")]
    partial class PrereleaseColumn
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.0.0-rc2-20896")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("NuGet.Services.Staging.Database.Models.PackageMetadata", b =>
                {
                    b.Property<int>("Key")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Authors");

                    b.Property<string>("Description");

                    b.Property<string>("IconUrl");

                    b.Property<string>("Id")
                        .IsRequired();

                    b.Property<bool>("IsPrerelease");

                    b.Property<string>("LicenseUrl");

                    b.Property<string>("Owners");

                    b.Property<string>("ProjectUrl");

                    b.Property<int>("StageKey");

                    b.Property<int>("StagedPackageKey");

                    b.Property<string>("Summary");

                    b.Property<string>("Tags");

                    b.Property<string>("Title");

                    b.Property<string>("Version")
                        .IsRequired();

                    b.HasKey("Key");

                    b.HasIndex("IsPrerelease");

                    b.HasIndex("StageKey");

                    b.HasIndex("StagedPackageKey");

                    b.ToTable("PackagesMetadata");
                });

            modelBuilder.Entity("NuGet.Services.Staging.Database.Models.Stage", b =>
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

            modelBuilder.Entity("NuGet.Services.Staging.Database.Models.StageCommit", b =>
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

            modelBuilder.Entity("NuGet.Services.Staging.Database.Models.StagedPackage", b =>
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

            modelBuilder.Entity("NuGet.Services.Staging.Database.Models.StageMembership", b =>
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

            modelBuilder.Entity("NuGet.Services.Staging.Database.Models.PackageMetadata", b =>
                {
                    b.HasOne("NuGet.Services.Staging.Database.Models.Stage")
                        .WithMany()
                        .HasForeignKey("StageKey")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("NuGet.Services.Staging.Database.Models.StagedPackage")
                        .WithOne()
                        .HasForeignKey("NuGet.Services.Staging.Database.Models.PackageMetadata", "StagedPackageKey")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NuGet.Services.Staging.Database.Models.StageCommit", b =>
                {
                    b.HasOne("NuGet.Services.Staging.Database.Models.Stage")
                        .WithMany()
                        .HasForeignKey("StageKey")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NuGet.Services.Staging.Database.Models.StagedPackage", b =>
                {
                    b.HasOne("NuGet.Services.Staging.Database.Models.Stage")
                        .WithMany()
                        .HasForeignKey("StageKey")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("NuGet.Services.Staging.Database.Models.StageMembership", b =>
                {
                    b.HasOne("NuGet.Services.Staging.Database.Models.Stage")
                        .WithMany()
                        .HasForeignKey("StageKey")
                        .OnDelete(DeleteBehavior.Cascade);
                });
        }
    }
}
