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
        public DbSet<Stage> Stages { get; set; }

        public DbSet<StageMember> StageMembers { get; set; }

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
                .HasMany(s => s.StageMembers);
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
                .HasIndex(sm => sm.UserKey);
            modelBuilder.Entity<StageMember>()
                .HasIndex(sm => sm.StageKey);
        }
    }
}
