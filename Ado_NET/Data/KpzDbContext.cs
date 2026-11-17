using Kpz.Lab2.Models;
using Microsoft.EntityFrameworkCore;

namespace Kpz.Lab2.Data;

// Контекст EF Core оголошує таблиці та підключає конфігурації сутностей із цієї збірки.
public sealed class KpzDbContext(DbContextOptions<KpzDbContext> options) : DbContext(options)
{
    public DbSet<TripCategory> TripCategories => Set<TripCategory>();
    public DbSet<ItineraryItemType> ItineraryItemTypes => Set<ItineraryItemType>();
    // DbSet надає доступ до користувачів і поїздок через LINQ-запити EF Core.
    public DbSet<User> Users => Set<User>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripMember> TripMembers => Set<TripMember>();
    public DbSet<ItineraryItem> ItineraryItems => Set<ItineraryItem>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<TripDocument> TripDocuments => Set<TripDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Застосовуємо окремі IEntityTypeConfiguration для таблиць, зв’язків та індексів.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KpzDbContext).Assembly);
    }
}
