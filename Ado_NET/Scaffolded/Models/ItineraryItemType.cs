using System;
using System.Collections.Generic;

namespace Kpz.Lab2.Scaffolded.Models;

public partial class ItineraryItemType
{
    public int ItineraryItemTypeId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<ItineraryItem> ItineraryItems { get; set; } = new List<ItineraryItem>();
}
