using Kpz.Lab2.Models;
using Web_API.Contracts;

namespace Web_API.BusinessRules;

/// <summary>Пункт маршруту має починатися й завершуватися в межах дат своєї поїздки.</summary>
public static class ItineraryItemDateRule
{
    public static bool IsSatisfied(Trip trip, ItineraryItemWriteDto item)
    {
        var startDate = DateOnly.FromDateTime(item.StartDateTime);
        var endDate = DateOnly.FromDateTime(item.EndDateTime);

        return startDate >= trip.StartDate && endDate <= trip.EndDate;
    }
}
