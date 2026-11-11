using Kpz.Lab2.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kpz.Lab2.Data;

// Налаштування таблиці користувачів: обов'язкові поля та унікальна електронна адреса.
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> entity)
    {
        entity.ToTable("Users");
        entity.HasKey(x => x.UserId);
        entity.Property(x => x.UserId).UseIdentityColumn();
        entity.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        entity.Property(x => x.Email).HasMaxLength(254).IsRequired();
        entity.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
        // Якщо час створення не задано, SQL Server поставить поточний UTC час.
        entity.Property(x => x.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("SYSUTCDATETIME()");
        entity.HasIndex(x => x.Email).IsUnique().HasDatabaseName("UX_Users_Email");
    }
}

// Налаштування подорожей, включно з перевіркою дат, власником і concurrency token.
internal sealed class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> entity)
    {
        entity.ToTable("Trips", table => table.HasCheckConstraint("CK_Trips_DateRange", "[EndDate] >= [StartDate]"));
        entity.HasKey(x => x.TripId);
        entity.Property(x => x.TripId).UseIdentityColumn();
        entity.Property(x => x.TripName).HasMaxLength(150).IsRequired();
        entity.Property(x => x.Destination).HasMaxLength(200).IsRequired();
        entity.Property(x => x.StartDate).HasColumnType("date").IsRequired();
        entity.Property(x => x.EndDate).HasColumnType("date").IsRequired();
        entity.Property(x => x.Description).HasMaxLength(2000);
        entity.Property(x => x.RowVersion).IsRowVersion();
        // Власника не можна видалити, доки на нього посилаються його подорожі.
        entity.HasOne(x => x.Owner).WithMany(x => x.OwnedTrips).HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_Trips_Users_OwnerId");
        entity.HasIndex(x => x.OwnerId).HasDatabaseName("IX_Trips_OwnerId");
    }
}

// Налаштування зв'язувальної таблиці учасників з комбінованим ключем.
internal sealed class TripMemberConfiguration : IEntityTypeConfiguration<TripMember>
{
    public void Configure(EntityTypeBuilder<TripMember> entity)
    {
        entity.ToTable("TripMembers");
        entity.HasKey(x => new { x.TripId, x.UserId });
        entity.Property(x => x.Role).HasMaxLength(30).IsRequired();
        entity.Property(x => x.JoinedAt).HasColumnType("datetime2").HasDefaultValueSql("SYSUTCDATETIME()");
        // Видалення подорожі прибирає її членства; користувача з членствами видалити не можна.
        entity.HasOne(x => x.Trip).WithMany(x => x.Members).HasForeignKey(x => x.TripId)
            .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_TripMembers_Trips_TripId");
        entity.HasOne(x => x.User).WithMany(x => x.TripMemberships).HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_TripMembers_Users_UserId");
        entity.HasIndex(x => x.UserId).HasDatabaseName("IX_TripMembers_UserId");
    }
}

// Пункти маршруту мають часовий інтервал, координати та належать одній подорожі.
internal sealed class ItineraryItemConfiguration : IEntityTypeConfiguration<ItineraryItem>
{
    public void Configure(EntityTypeBuilder<ItineraryItem> entity)
    {
        entity.ToTable("ItineraryItems", table => table.HasCheckConstraint(
            "CK_ItineraryItems_DateRange", "[EndDateTime] >= [StartDateTime]"));
        entity.HasKey(x => x.ItemId);
        entity.Property(x => x.ItemId).UseIdentityColumn();
        entity.Property(x => x.ItemType).HasMaxLength(40).IsRequired();
        entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Description).HasMaxLength(2000);
        entity.Property(x => x.StartDateTime).HasColumnType("datetime2").IsRequired();
        entity.Property(x => x.EndDateTime).HasColumnType("datetime2").IsRequired();
        entity.Property(x => x.Address).HasMaxLength(400);
        entity.Property(x => x.Latitude).HasPrecision(9, 6);
        entity.Property(x => x.Longitude).HasPrecision(9, 6);
        entity.HasOne(x => x.Trip).WithMany(x => x.ItineraryItems).HasForeignKey(x => x.TripId)
            .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_ItineraryItems_Trips_TripId");
        entity.HasIndex(x => new { x.TripId, x.StartDateTime }).HasDatabaseName("IX_ItineraryItems_TripId_StartDateTime");
    }
}

// Бронювання має невід'ємну ціну та належить пункту маршруту.
internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> entity)
    {
        entity.ToTable("Bookings", table => table.HasCheckConstraint("CK_Bookings_Price", "[Price] >= 0"));
        entity.HasKey(x => x.BookingId);
        entity.Property(x => x.BookingId).UseIdentityColumn();
        entity.Property(x => x.BookingNumber).HasMaxLength(100).IsRequired();
        entity.Property(x => x.Provider).HasMaxLength(150).IsRequired();
        entity.Property(x => x.BookingDate).HasColumnType("datetime2").IsRequired();
        entity.Property(x => x.Price).HasPrecision(18, 2).IsRequired();
        entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
        entity.Property(x => x.ConfirmationDetails).HasMaxLength(2000);
        entity.HasOne(x => x.Item).WithMany(x => x.Bookings).HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_Bookings_ItineraryItems_ItemId");
        entity.HasIndex(x => x.ItemId).HasDatabaseName("IX_Bookings_ItemId");
        entity.HasIndex(x => x.BookingNumber).HasDatabaseName("IX_Bookings_BookingNumber");
    }
}

// Документи належать подорожі; шлях до файлу зберігається як текстове поле.
internal sealed class TripDocumentConfiguration : IEntityTypeConfiguration<TripDocument>
{
    public void Configure(EntityTypeBuilder<TripDocument> entity)
    {
        entity.ToTable("TripDocuments");
        entity.HasKey(x => x.DocumentId);
        entity.Property(x => x.DocumentId).UseIdentityColumn();
        entity.Property(x => x.DocumentName).HasMaxLength(200).IsRequired();
        entity.Property(x => x.DocumentType).HasMaxLength(50).IsRequired();
        entity.Property(x => x.FilePath).HasMaxLength(1000).IsRequired();
        entity.Property(x => x.IsRequired).IsRequired();
        entity.Property(x => x.ExpirationDate).HasColumnType("date");
        entity.HasOne(x => x.Trip).WithMany(x => x.Documents).HasForeignKey(x => x.TripId)
            .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_TripDocuments_Trips_TripId");
        entity.HasIndex(x => x.TripId).HasDatabaseName("IX_TripDocuments_TripId");
    }
}
