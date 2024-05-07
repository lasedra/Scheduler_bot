using System;
using System.Collections.Generic;

namespace Scheduler_bot.Models;

public partial class Cabinet
{
    public string Number { get; set; } = null!;

    public string? Name { get; set; }

    public virtual ICollection<DailyScheduleBody> DailyScheduleBodies { get; set; } = new List<DailyScheduleBody>();
}
