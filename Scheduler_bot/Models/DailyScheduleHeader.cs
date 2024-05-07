using System;
using System.Collections.Generic;

namespace Scheduler_bot.Models;

public partial class DailyScheduleHeader
{
    public string StudentGroupCode { get; set; } = null!;

    public DateOnly OfDate { get; set; }

    public virtual ICollection<DailyScheduleBody> DailyScheduleBodies { get; set; } = new List<DailyScheduleBody>();

    public virtual StudentGroup StudentGroupCodeNavigation { get; set; } = null!;
}
