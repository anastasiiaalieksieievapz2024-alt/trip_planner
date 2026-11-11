using System;
using System.Collections.Generic;

namespace Kpz.Lab2.Scaffolded.Models;

public partial class ItineraryItem
{
    public int ItemId { get; set; }

    public int TripId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime StartDateTime { get; set; }

    public DateTime EndDateTime { get; set; }

    public string? Address { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public int ItineraryItemTypeId { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ItineraryItemType ItineraryItemType { get; set; } = null!;

    public virtual Trip Trip { get; set; } = null!;
}
