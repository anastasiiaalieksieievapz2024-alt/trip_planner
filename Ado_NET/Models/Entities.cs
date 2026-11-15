namespace Kpz.Lab2.Models;

// ������� �������� ��� ���������� �� ��������� ���������.
public sealed class TripCategory
{
    public int TripCategoryId { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public ICollection<Trip> Trips { get; } = new List<Trip>();
}

// ������� ���� ������ ��������: ���������, ����������, ��������� ����.
public sealed class ItineraryItemType
{
    public int ItineraryItemTypeId { get; set; }
    public required string Name { get; set; }
    public ICollection<ItineraryItem> Items { get; } = new List<ItineraryItem>();
}

// �������� ����� �����������, ���� ���� ���������� ������� �� ���������� �� ���.
public sealed class User
{
    // ��������� ���� �����������.
    public int UserId { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    // ���������� ��� ������, � �� ��� ������.
    public required string PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; }
    // ��������� ����������: �������, �� ���������� ������� ��� �������.
    public ICollection<Trip> OwnedTrips { get; } = new List<Trip>();
    public ICollection<TripMember> TripMemberships { get; } = new List<TripMember>();
}

// ������� �� ��������, ���� �� ���'���� ������ ��������, �������� � ���������.
public sealed class Trip
{
    public int TripId { get; set; }
    // ������� ���� �� �����������-��������.
    public int OwnerId { get; set; }
    // ������� ���� �� ������� ��������.
    public int TripCategoryId { get; set; }
    public required string TripName { get; set; }
    public required string Destination { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    // ���� ������'�������, ���� ��� ������� null.
    public string? Description { get; set; }
    // ������'����� ������� ����������; ������� ���������� ������������ EF Core.
    public string? PlanningNotes { get; set; }
    // SQL Server ����� �� �������� ��� �������� ����� ��� ��������� �������� ������.
    public byte[] RowVersion { get; set; } = [];
    // ��������� ���������� ��� ��'���� EF Core.
    public User Owner { get; set; } = null!;
    public TripCategory Category { get; set; } = null!;
    public ICollection<TripMember> Members { get; } = new List<TripMember>();
    public ICollection<ItineraryItem> ItineraryItems { get; } = new List<ItineraryItem>();
    public ICollection<TripDocument> Documents { get; } = new List<TripDocument>();
}

// ������� ������� ��� ��'���� ������-��-�������� �� ������������� �� ����������.
// ��������� ��������� ���� (TripId, UserId) �� �������� ������ ���� ������ ����������� ����.
public sealed class TripMember
{
    public int TripId { get; set; }
    public int UserId { get; set; }
    public required string Role { get; set; }
    public DateTime JoinedAt { get; set; }
    public Trip Trip { get; set; } = null!;
    public User User { get; set; } = null!;
}

// ������� ����� ��������: ������, ����������, �������� �� ���������.
public sealed class ItineraryItem
{
    public int ItemId { get; set; }
    public int TripId { get; set; }
    // ������� ���� �� ������� ���� ������ ��������.
    public int ItineraryItemTypeId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    // ����� �������� ���� ������� �� ���� ���� ����� ���������.
    public Trip Trip { get; set; } = null!;
    public ItineraryItemType Type { get; set; } = null!;
    public ICollection<Booking> Bookings { get; } = new List<Booking>();
}

// ����������, ���'����� � ���������� ������� ��������.
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

// ��������, ���� �������� �������; ���� ���������� 䳿 ���� ���� ����������.
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
