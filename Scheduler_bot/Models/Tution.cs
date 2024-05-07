using System;
using System.Collections.Generic;

namespace Scheduler_bot.Models;

public partial class Tution
{
    public Guid SubjectId { get; set; }

    public Guid EmployeeId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public virtual Employee Employee { get; set; } = null!;

    public virtual Subject Subject { get; set; } = null!;
}
