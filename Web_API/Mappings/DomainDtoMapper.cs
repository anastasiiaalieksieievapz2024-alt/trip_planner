using Kpz.Lab2.Models;
using Web_API.Contracts;

namespace Web_API.Mappings;

/// <summary>Ручний мапінг між EF доменними сутностями та API DTO.</summary>
public static class DomainDtoMapper
{
    public static TripReadDto ToReadDto(Trip trip) => new(
        trip.TripId, trip.TripName, trip.Destination, trip.StartDate, trip.EndDate,
        trip.Description, trip.TripCategoryId, trip.Category.Name);

    public static void Apply(TripWriteDto source, Trip target)
    {
        target.TripCategoryId = source.TripCategoryId;
        target.TripName = source.TripName;
        target.Destination = source.Destination;
        target.StartDate = source.StartDate;
        target.EndDate = source.EndDate;
        target.Description = source.Description;
        target.PlanningNotes = source.PlanningNotes;
    }

    public static ItineraryItemReadDto ToReadDto(
        ItineraryItem item, string type, IReadOnlyList<BookingConfirmation> bookings) => new(
        item.ItemId, type, item.Title, item.Description, item.StartDateTime,
        item.EndDateTime, item.Address, item.Latitude, item.Longitude, bookings);

    public static ItineraryItem ToEntity(CreateItineraryItemDto source, int tripId) => new()
    {
        TripId = tripId,
        ItineraryItemTypeId = source.ItineraryItemTypeId,
        Title = source.Title,
        Description = source.Description,
        StartDateTime = source.StartDateTime,
        EndDateTime = source.EndDateTime,
        Address = source.Address,
        Latitude = source.Latitude,
        Longitude = source.Longitude
    };

    public static void Apply(UpdateItineraryItemDto source, ItineraryItem target)
    {
        target.ItineraryItemTypeId = source.ItineraryItemTypeId;
        target.Title = source.Title;
        target.Description = source.Description;
        target.StartDateTime = source.StartDateTime;
        target.EndDateTime = source.EndDateTime;
        target.Address = source.Address;
        target.Latitude = source.Latitude;
        target.Longitude = source.Longitude;
    }

    public static TripMemberReadDto ToReadDto(TripMember member) => new(
        member.UserId, member.User.FullName, member.User.Email, member.Role, member.JoinedAt);
}


