using Microsoft.Extensions.Configuration;
using PRTelegramBot.Models;
using PRTelegramBot.Utils;
using Scheduler_bot.Models;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot;
using Telegram.Bot.Types;
using Helpers = PRTelegramBot.Helpers;

namespace Scheduler_bot
{
    public static class Dispatcher
    {
        public static IConfiguration AppConfig { get; set; } = new ConfigurationBuilder().AddJsonFile("appconfig.json", optional: false, reloadOnChange: true).Build();

        public static SchedulerDbContext DbContext { get; set; } = new (AppConfig.GetConnectionString("localhost") ?? "NO_CONNECTION_STRING");

        public static class CurrentUser
        {
            public static Guid EmployeeId { get; set; }

            public static bool WorkingStatus { get; set; }

            public static string Name { get; set; } = null!;

            public static bool Role { get; set; }

            public static string Login { get; set; } = null!;

            public static string Password { get; set; } = null!;

            public static string PhoneNumber { get; set; } = null!;

            public static string? EMail { get; set; }

            public static bool TelegramConfirmed { get; set; }

            public static bool SetUser(Employee loggingEmployee)
            {
                EmployeeId = loggingEmployee.EmployeeId;
                Name = loggingEmployee.Name;
                Role = loggingEmployee.Role;
                Login = loggingEmployee.Login;
                Password = loggingEmployee.Password;
                TelegramConfirmed = loggingEmployee.TelegramConfirmed;
                PhoneNumber = loggingEmployee.PhoneNumber;
                EMail = loggingEmployee.EMail is not null ? loggingEmployee.EMail : "Почта не указана";

                return true;
            }

            public static string GetRoleString()
            {
                return Role ? "менеджер учебного процесса" : "преподаватель";
            }
        }

        public static async Task ShowMainMenu(ITelegramBotClient botClient, Update update)
        {
            var menuContent = new List<KeyboardButton>()
            {
                new("Назначить занятие⤵"),
                new("..."),
                new("...")
            };
            var menu = MenuGenerator.ReplyKeyboard(1, menuContent);

            await Helpers.Message.Send(botClient, update,
                msg: "Чем могу быть полезен?",
                option: new OptionMessage() { MenuReplyKeyboardMarkup = menu });
        }
    }
}
