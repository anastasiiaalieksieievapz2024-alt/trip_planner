using System;
using System.Collections.Generic;

namespace Kpz.Lab2.Scaffolded.Models;

public partial class Booking
{
    public int BookingId { get; set; }

    public int ItemId { get; set; }

    public string BookingNumber { get; set; } = null!;

    public string Provider { get; set; } = null!;

    public DateTime BookingDate { get; set; }

    public decimal Price { get; set; }

    public string Status { get; set; } = null!;

    public string? ConfirmationDetails { get; set; }

    public virtual ItineraryItem Item { get; set; } = null!;
}
