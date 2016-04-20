// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Data.Entity;

namespace NuGet.Services.Staging.Database.Models
{
    /// <summary>
    /// Useful links:
    /// http://ef.readthedocs.org/en/latest/platforms/aspnetcore/new-db.html
    /// </summary>
    public class StageContext : DbContext
    {
        public virtual DbSet<Stage> Stages { get; set; }

        public virtual DbSet<StageMembership> StageMemberships { get; set; }

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
                .HasMany(s => s.Memberships);
            modelBuilder.Entity<Stage>()
                .HasMany(s => s.Packages);
            modelBuilder.Entity<Stage>()
                .HasMany(s => s.Commits);
            modelBuilder.Entity<Stage>()
                .HasIndex(s => s.Id);

            modelBuilder.Entity<StageMembership>()
                .HasKey(sm => sm.Key);
            modelBuilder.Entity<StageMembership>()
                .Property(sm => sm.StageKey).IsRequired();
            modelBuilder.Entity<StageMembership>()
                .Property(sm => sm.UserKey).IsRequired();
            modelBuilder.Entity<StageMembership>()
                .Property(sm => sm.MembershipType).IsRequired();
            modelBuilder.Entity<StageMembership>()
                .HasOne(sm => sm.Stage)
                .WithMany(s => s.Memberships);
            modelBuilder.Entity<StageMembership>()
                .HasIndex(sm => sm.UserKey);
            modelBuilder.Entity<StageMembership>()
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
                .Property(sp => sp.NuspecUrl).IsRequired();
            modelBuilder.Entity<StagedPackage>()
                .Property(sp => sp.Published).IsRequired();
            modelBuilder.Entity<StagedPackage>()
                .Property(sp => sp.StageKey).IsRequired();
            modelBuilder.Entity<StagedPackage>()
                .HasIndex(sp => sp.StageKey);

            modelBuilder.Entity<StageCommit>()
                .HasKey(sc => sc.Key);
            modelBuilder.Entity<StageCommit>()
                .Property(sc => sc.StageKey).IsRequired();
            modelBuilder.Entity<StageCommit>()
                .Property(sc => sc.TrackId).IsRequired().HasMaxLength(32);
            modelBuilder.Entity<StageCommit>()
                .Property(sc => sc.RequestTime).IsRequired();
            modelBuilder.Entity<StageCommit>()
                .Property(sc => sc.Status).IsRequired();
            modelBuilder.Entity<StageCommit>()
                .HasIndex(sc => sc.StageKey);
        }
    }
}
