using System;
using System.Collections.Generic;

namespace Scheduler_bot.Models;

public partial class ClassesTimingHeader
{
    public Guid ClassesTimingHeaderId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<ClassesTimingBody> ClassesTimingBodies { get; set; } = new List<ClassesTimingBody>();
}
