using System;
using System.Collections.Generic;

namespace Kpz.Lab2.Scaffolded.Models;

public partial class Trip
{
    public int TripId { get; set; }

    public int OwnerId { get; set; }

    public string TripName { get; set; } = null!;

    public string Destination { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string? Description { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public int TripCategoryId { get; set; }

    public string? PlanningNotes { get; set; }

    public virtual ICollection<ItineraryItem> ItineraryItems { get; set; } = new List<ItineraryItem>();

    public virtual User Owner { get; set; } = null!;

    public virtual TripCategory TripCategory { get; set; } = null!;

    public virtual ICollection<TripDocument> TripDocuments { get; set; } = new List<TripDocument>();

    public virtual ICollection<TripMember> TripMembers { get; set; } = new List<TripMember>();
}
