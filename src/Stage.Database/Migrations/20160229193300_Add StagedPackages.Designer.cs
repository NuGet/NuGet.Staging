using System;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Migrations;
using Stage.Database.Models;

namespace Stage.Database.Migrations
{
    [DbContext(typeof(StageContext))]
    [Migration("20160229193300_Add StagedPackages")]
    partial class AddStagedPackages
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.0-rc1-16348")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Stage.Database.Models.Stage", b =>
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
                });

            modelBuilder.Entity("Stage.Database.Models.StagedPackage", b =>
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

                    b.Property<DateTime>("PushDate");

                    b.Property<int>("StageKey");

                    b.Property<int>("UserKey");

                    b.Property<string>("Version")
                        .IsRequired()
                        .HasAnnotation("MaxLength", 64);

                    b.HasKey("Key");

                    b.HasIndex("StageKey");
                });

            modelBuilder.Entity("Stage.Database.Models.StageMember", b =>
                {
                    b.Property<int>("Key")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("MemberType");

                    b.Property<int>("StageKey");

                    b.Property<int>("UserKey");

                    b.HasKey("Key");

                    b.HasIndex("StageKey");

                    b.HasIndex("UserKey");
                });

            modelBuilder.Entity("Stage.Database.Models.StagedPackage", b =>
                {
                    b.HasOne("Stage.Database.Models.Stage")
                        .WithMany()
                        .HasForeignKey("StageKey");
                });

            modelBuilder.Entity("Stage.Database.Models.StageMember", b =>
                {
                    b.HasOne("Stage.Database.Models.Stage")
                        .WithMany()
                        .HasForeignKey("StageKey");
                });
        }
    }
}
