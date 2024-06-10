using System;
using System.Collections.Generic;

namespace Scheduler_bot.Models;

public partial class Subject
{
    public Guid SubjectId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<DailyScheduleBody> DailyScheduleBodies { get; set; } = new List<DailyScheduleBody>();

    public virtual ICollection<Studying> Studyings { get; set; } = new List<Studying>();

    public virtual ICollection<Tution> Tutions { get; set; } = new List<Tution>();
}
