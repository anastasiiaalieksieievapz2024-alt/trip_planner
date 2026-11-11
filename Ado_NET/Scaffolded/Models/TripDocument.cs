using System;
using System.Collections.Generic;

namespace Kpz.Lab2.Scaffolded.Models;

public partial class TripDocument
{
    public int DocumentId { get; set; }

    public int TripId { get; set; }

    public string DocumentName { get; set; } = null!;

    public string DocumentType { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public bool IsRequired { get; set; }

    public DateOnly? ExpirationDate { get; set; }

    public virtual Trip Trip { get; set; } = null!;
}
