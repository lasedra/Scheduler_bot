using System;
using System.Collections.Generic;

namespace Scheduler_bot.Models;

public partial class DailyScheduleBody
{
    public string StudentGroupCode { get; set; } = null!;

    public DateOnly OfDate { get; set; }

    public int ClassNumber { get; set; }

    public Guid ClassesTimingHeaderId { get; set; }

    public Guid? EmployeeId { get; set; }

    public Guid? SubjectId { get; set; }

    public string? CabinetNumber { get; set; }

    public virtual Cabinet? CabinetNumberNavigation { get; set; }

    public virtual DailyScheduleHeader DailyScheduleHeader { get; set; } = null!;

    public virtual Employee? Employee { get; set; }

    public virtual Subject? Subject { get; set; }
}
