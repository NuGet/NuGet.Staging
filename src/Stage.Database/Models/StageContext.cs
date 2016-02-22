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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Stage>()
                .HasKey(s => s.Key);
            modelBuilder.Entity<Stage>()
                .Property(s => s.Name).IsRequired();
            modelBuilder.Entity<Stage>()
                .Property(s => s.CreationDate).IsRequired();
            modelBuilder.Entity<Stage>()
                .Property(s => s.ExpirationDate).IsRequired();
            modelBuilder.Entity<Stage>()
                .Property(s => s.Status).IsRequired();
            modelBuilder.Entity<Stage>().
                HasMany(s => s.StageMemebers);

            modelBuilder.Entity<StageMemeber>()
                .HasKey(sm => sm.Key);
            modelBuilder.Entity<StageMemeber>()
                .Property(sm => sm.StageKey).IsRequired();
            modelBuilder.Entity<StageMemeber>()
                .Property(sm => sm.UserKey).IsRequired();
            modelBuilder.Entity<StageMemeber>()
                .Property(sm => sm.MemberType).IsRequired();
        }
    }
}
