// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Data.Entity;

namespace Stage.Database.Models
{
    /// <summary>
    /// Useful links:
    /// http://ef.readthedocs.org/en/latest/platforms/aspnetcore/new-db.html
    /// </summary>
    public class StageContext : DbContext
    {
        public virtual DbSet<Stage> Stages { get; set; }

        public virtual DbSet<StageMember> StageMembers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Stage>()
                .HasKey(s => s.Key);
            modelBuilder.Entity<Stage>()
                .Property(s => s.Id).IsRequired().HasMaxLength(32);
            modelBuilder.Entity<Stage>()
                .Property(s => s.DisplayName).IsRequired();
            modelBuilder.Entity<Stage>()
                .Property(s => s.CreationDate).IsRequired();
            modelBuilder.Entity<Stage>()
                .Property(s => s.ExpirationDate).IsRequired();
            modelBuilder.Entity<Stage>()
                .Property(s => s.Status).IsRequired();
            modelBuilder.Entity<Stage>()
                .HasMany(s => s.Members);
            modelBuilder.Entity<Stage>()
                .HasMany(s => s.Packages);
            modelBuilder.Entity<Stage>()
                .HasIndex(s => s.Id);

           modelBuilder.Entity<StageMember>()
                .HasKey(sm => sm.Key);
            modelBuilder.Entity<StageMember>()
                .Property(sm => sm.StageKey).IsRequired();
            modelBuilder.Entity<StageMember>()
                .Property(sm => sm.UserKey).IsRequired();
            modelBuilder.Entity<StageMember>()
                .Property(sm => sm.MemberType).IsRequired();
            modelBuilder.Entity<StageMember>()
                .HasOne(sm => sm.Stage)
                .WithMany(s => s.Members);
            modelBuilder.Entity<StageMember>()
                .HasIndex(sm => sm.UserKey);
            modelBuilder.Entity<StageMember>()
                .HasIndex(sm => sm.StageKey);

            // TODO: take consts from common lib
            modelBuilder.Entity<StagedPackage>()
                .HasKey(sp => sp.Key);
            modelBuilder.Entity<StagedPackage>()
                .Property(sp => sp.Id).IsRequired().HasMaxLength(128);
            modelBuilder.Entity<StagedPackage>()
                .Property(sp => sp.Version).IsRequired().HasMaxLength(64);
            modelBuilder.Entity<StagedPackage>()
               .Property(sp => sp.NormalizedVersion).IsRequired().HasMaxLength(64);
            modelBuilder.Entity<StagedPackage>()
              .Property(sp => sp.NupkgUrl).IsRequired();
            modelBuilder.Entity<StagedPackage>()
              .Property(sp => sp.Published).IsRequired();
            modelBuilder.Entity<StagedPackage>()
              .Property(sp => sp.StageKey).IsRequired();
            modelBuilder.Entity<StagedPackage>()
                .HasIndex(sp => sp.StageKey);

        }
    }
}
