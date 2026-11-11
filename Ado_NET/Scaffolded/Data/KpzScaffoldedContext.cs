using System;
using System.Collections.Generic;
using Kpz.Lab2.Scaffolded.Models;
using Microsoft.EntityFrameworkCore;

namespace Kpz.Lab2.Scaffolded.Data;

public partial class KpzScaffoldedContext : DbContext
{
    public KpzScaffoldedContext(DbContextOptions<KpzScaffoldedContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<ItineraryItem> ItineraryItems { get; set; }

    public virtual DbSet<ItineraryItemType> ItineraryItemTypes { get; set; }

    public virtual DbSet<Trip> Trips { get; set; }

    public virtual DbSet<TripCategory> TripCategories { get; set; }

    public virtual DbSet<TripDocument> TripDocuments { get; set; }

    public virtual DbSet<TripMember> TripMembers { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasIndex(e => e.BookingNumber, "IX_Bookings_BookingNumber");

            entity.HasIndex(e => e.ItemId, "IX_Bookings_ItemId");

            entity.Property(e => e.BookingNumber).HasMaxLength(100);
            entity.Property(e => e.ConfirmationDetails).HasMaxLength(2000);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Provider).HasMaxLength(150);
            entity.Property(e => e.Status).HasMaxLength(30);

            entity.HasOne(d => d.Item).WithMany(p => p.Bookings).HasForeignKey(d => d.ItemId);
        });

        modelBuilder.Entity<ItineraryItem>(entity =>
        {
            entity.HasKey(e => e.ItemId);

            entity.HasIndex(e => e.ItineraryItemTypeId, "IX_ItineraryItems_ItineraryItemTypeId");

            entity.HasIndex(e => new { e.TripId, e.StartDateTime }, "IX_ItineraryItems_TripId_StartDateTime");

            entity.Property(e => e.Address).HasMaxLength(400);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Latitude).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.ItineraryItemType).WithMany(p => p.ItineraryItems)
                .HasForeignKey(d => d.ItineraryItemTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Trip).WithMany(p => p.ItineraryItems).HasForeignKey(d => d.TripId);
        });

        modelBuilder.Entity<ItineraryItemType>(entity =>
        {
            entity.HasIndex(e => e.Name, "UX_ItineraryItemTypes_Name").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(40);
        });

        modelBuilder.Entity<Trip>(entity =>
        {
            entity.HasIndex(e => e.OwnerId, "IX_Trips_OwnerId");

            entity.HasIndex(e => e.TripCategoryId, "IX_Trips_TripCategoryId");

            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Destination).HasMaxLength(200);
            entity.Property(e => e.PlanningNotes).HasMaxLength(1000);
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.TripName).HasMaxLength(150);

            entity.HasOne(d => d.Owner).WithMany(p => p.Trips)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.TripCategory).WithMany(p => p.Trips)
                .HasForeignKey(d => d.TripCategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<TripCategory>(entity =>
        {
            entity.HasIndex(e => e.Name, "UX_TripCategories_Name").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.Name).HasMaxLength(80);
        });

        modelBuilder.Entity<TripDocument>(entity =>
        {
            entity.HasKey(e => e.DocumentId);

            entity.HasIndex(e => e.TripId, "IX_TripDocuments_TripId");

            entity.Property(e => e.DocumentName).HasMaxLength(200);
            entity.Property(e => e.DocumentType).HasMaxLength(50);
            entity.Property(e => e.FilePath).HasMaxLength(1000);

            entity.HasOne(d => d.Trip).WithMany(p => p.TripDocuments).HasForeignKey(d => d.TripId);
        });

        modelBuilder.Entity<TripMember>(entity =>
        {
            entity.HasKey(e => new { e.TripId, e.UserId });

            entity.HasIndex(e => e.UserId, "IX_TripMembers_UserId");

            entity.Property(e => e.JoinedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Role).HasMaxLength(30);

            entity.HasOne(d => d.Trip).WithMany(p => p.TripMembers).HasForeignKey(d => d.TripId);

            entity.HasOne(d => d.User).WithMany(p => p.TripMembers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email, "UX_Users_Email").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Email).HasMaxLength(254);
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.PasswordHash).HasMaxLength(512);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
