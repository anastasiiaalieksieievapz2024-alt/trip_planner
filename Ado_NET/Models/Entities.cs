namespace Kpz.Lab2.Models;

// Обліковий запис користувача, який може створювати подорожі та долучатися до них.
public sealed class User
{
    // Первинний ключ користувача.
    public int UserId { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    // Зберігається хеш пароля, а не сам пароль.
    public required string PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; }
    // Навігаційні властивості: подорожі, де користувач власник або учасник.
    public ICollection<Trip> OwnedTrips { get; } = new List<Trip>();
    public ICollection<TripMember> TripMemberships { get; } = new List<TripMember>();
}

// Подорож має власника, дати та пов'язані записи маршруту, учасників і документів.
public sealed class Trip
{
    public int TripId { get; set; }
    // Зовнішній ключ на користувача-власника.
    public int OwnerId { get; set; }
    public required string TripName { get; set; }
    public required string Destination { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    // Опис необов'язковий, тому тип допускає null.
    public string? Description { get; set; }
    // SQL Server змінює це значення при оновленні рядка для виявлення конфліктів запису.
    public byte[] RowVersion { get; set; } = [];
    // Навігаційні властивості для зв'язків EF Core.
    public User Owner { get; set; } = null!;
    public ICollection<TripMember> Members { get; } = new List<TripMember>();
    public ICollection<ItineraryItem> ItineraryItems { get; } = new List<ItineraryItem>();
    public ICollection<TripDocument> Documents { get; } = new List<TripDocument>();
}

// Проміжна сутність для зв'язку багато-до-багатьох між користувачами та подорожами.
// Складений первинний ключ (TripId, UserId) не дозволяє додати того самого користувача двічі.
public sealed class TripMember
{
    public int TripId { get; set; }
    public int UserId { get; set; }
    public required string Role { get; set; }
    public DateTime JoinedAt { get; set; }
    public Trip Trip { get; set; } = null!;
    public User User { get; set; } = null!;
}

// Окремий пункт маршруту: переліт, проживання, трансфер чи активність.
public sealed class ItineraryItem
{
    public int ItemId { get; set; }
    public int TripId { get; set; }
    public required string ItemType { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    // Пункт належить одній подорожі та може мати кілька бронювань.
    public Trip Trip { get; set; } = null!;
    public ICollection<Booking> Bookings { get; } = new List<Booking>();
}

// Бронювання, пов'язане з конкретним пунктом маршруту.
public sealed class Booking
{
    public int BookingId { get; set; }
    public int ItemId { get; set; }
    public required string BookingNumber { get; set; }
    public required string Provider { get; set; }
    public DateTime BookingDate { get; set; }
    public decimal Price { get; set; }
    public required string Status { get; set; }
    public string? ConfirmationDetails { get; set; }
    public ItineraryItem Item { get; set; } = null!;
}

// Документ, який належить подорожі; дата завершення дії може бути невказаною.
public sealed class TripDocument
{
    public int DocumentId { get; set; }
    public int TripId { get; set; }
    public required string DocumentName { get; set; }
    public required string DocumentType { get; set; }
    public required string FilePath { get; set; }
    public bool IsRequired { get; set; }
    public DateOnly? ExpirationDate { get; set; }
    public Trip Trip { get; set; } = null!;
}
