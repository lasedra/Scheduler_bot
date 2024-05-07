using System;
using System.Collections.Generic;

namespace Scheduler_bot.Models;

public partial class Studying
{
    public string StudentGroupCode { get; set; } = null!;

    public Guid SubjectId { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public virtual StudentGroup StudentGroupCodeNavigation { get; set; } = null!;

    public virtual Subject Subject { get; set; } = null!;
}
