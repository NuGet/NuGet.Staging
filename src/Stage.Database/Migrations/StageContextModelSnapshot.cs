using System;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Migrations;
using Stage.Database.Models;

namespace Stage.Manager.Migrations
{
    [DbContext(typeof(StageContext))]
    partial class StageContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
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
                        .IsRequired();

                    b.Property<int>("Status");

                    b.HasKey("Key");
                });

            modelBuilder.Entity("Stage.Database.Models.StageMemeber", b =>
                {
                    b.Property<int>("Key")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("MemberType");

                    b.Property<int>("StageKey");

                    b.Property<int>("UserKey");

                    b.HasKey("Key");
                });

            modelBuilder.Entity("Stage.Database.Models.StageMemeber", b =>
                {
                    b.HasOne("Stage.Database.Models.Stage")
                        .WithMany()
                        .HasForeignKey("StageKey");
                });
        }
    }
}
