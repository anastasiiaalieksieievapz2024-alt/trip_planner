using Kpz.Lab2.Models;
using Microsoft.EntityFrameworkCore;

namespace Kpz.Lab2.Data;

// ����������� ���� EF Core: ���� ������ �� ������� � ����� ���� �����������.
public sealed class KpzDbContext(DbContextOptions<KpzDbContext> options) : DbContext(options)
{
    public DbSet<TripCategory> TripCategories => Set<TripCategory>();
    public DbSet<ItineraryItemType> ItineraryItemTypes => Set<ItineraryItemType>();
    // DbSet ������� ������� �� �������� ���������� LINQ-������ �� �� ������.
    public DbSet<User> Users => Set<User>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripMember> TripMembers => Set<TripMember>();
    public DbSet<ItineraryItem> ItineraryItems => Set<ItineraryItem>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<TripDocument> TripDocuments => Set<TripDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ��������� ����������� IEntityTypeConfiguration � ���� �����.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KpzDbContext).Assembly);
    }
}
