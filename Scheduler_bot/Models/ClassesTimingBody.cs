using System;
using System.Collections.Generic;

namespace Scheduler_bot.Models;

public partial class ClassesTimingBody
{
    public Guid ClassesTimingHeaderId { get; set; }

    public int ClassNumber { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public virtual ClassesTimingHeader ClassesTimingHeader { get; set; } = null!;
}
