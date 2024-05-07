using System;
using System.Collections.Generic;

namespace Scheduler_bot.Models;

public partial class Employee
{
    public Guid EmployeeId { get; set; }

    public bool WorkingStatus { get; set; }

    public string Name { get; set; } = null!;

    public bool Role { get; set; }

    public string Login { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string TelegramId { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string? EMail { get; set; }

    public virtual ICollection<DailyScheduleBody> DailyScheduleBodies { get; set; } = new List<DailyScheduleBody>();

    public virtual ICollection<Tution> Tutions { get; set; } = new List<Tution>();
}
