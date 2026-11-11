using System;
using System.Collections.Generic;

namespace Kpz.Lab2.Scaffolded.Models;

public partial class TripMember
{
    public int TripId { get; set; }

    public int UserId { get; set; }

    public string Role { get; set; } = null!;

    public DateTime JoinedAt { get; set; }

    public virtual Trip Trip { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
