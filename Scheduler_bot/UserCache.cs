﻿using PRTelegramBot.Interface;
using Scheduler_bot.Models;

namespace Scheduler_bot
{
    public class UserCache : ITelegramCache
    {
        public Guid EmployeeId { get; set; }

        public bool WorkingStatus { get; set; }

        public string Name { get; set; } = null!;

        public bool Role { get; set; }

        public string Login { get; set; } = null!;

        public string Password { get; set; } = null!;

        //TODO: TelegramId может отсутствовать
        public static string tempTelegramId { get; set; } = null!;
        public string TelegramId { get; set; } = null!;

        public string PhoneNumber { get; set; } = null!;

        public string? EMail { get; set; }

        public virtual ICollection<DailyScheduleBody> DailyScheduleBodies { get; set; } = new List<DailyScheduleBody>();

        public virtual ICollection<Tution> Tutions { get; set; } = new List<Tution>();

        public bool SetUser(Employee loggingEmployee)
        {
            EmployeeId = loggingEmployee.EmployeeId;
            Name = loggingEmployee.Name;
            Role = loggingEmployee.Role;
            Login = loggingEmployee.Login;
            Password = loggingEmployee.Password;
            TelegramId = loggingEmployee.TelegramId;
            PhoneNumber = loggingEmployee.PhoneNumber;
            EMail = loggingEmployee.EMail is not null ? loggingEmployee.EMail : "Почта не указана";

            return true;
        }

        public string GetRoleString()
        {
            return this.Role ? "менеджер учебного процесса" : "преподаватель";
        }

        public bool ClearData()
        {
            EmployeeId = Guid.Empty;
            WorkingStatus = false;
            Name = string.Empty;
            Role = false;
            Login = string.Empty;
            Password = string.Empty;
            TelegramId = string.Empty;
            PhoneNumber = string.Empty;
            EMail = string.Empty;
            return true;
        }
    }
}
