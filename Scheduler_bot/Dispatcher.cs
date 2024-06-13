using Microsoft.Extensions.Configuration;
using PRTelegramBot.Models;
using PRTelegramBot.Utils;
using Scheduler_bot.Models;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot;
using Telegram.Bot.Types;
using Helpers = PRTelegramBot.Helpers;
using PRTelegramBot.Attributes;

namespace Scheduler_bot
{
    public static class Dispatcher
    {
        public static IConfiguration AppConfig { get; set; } = new ConfigurationBuilder().AddJsonFile("appconfig.json", optional: false, reloadOnChange: true).Build();

        public static SchedulerDbContext DbContext { get; set; } = new() { appConfig = AppConfig };

        public class TimePeriod
        {
            public DateOnly TodayDate { get; set; }
            public DateOnly WeekStart { get; set; }
            public DateOnly WeekEnd { get; set; }
            public DateOnly SchoolyearStart { get; set; }
            public DateOnly SchoolyearEnd { get; set; }

            public TimePeriod(DateOnly todayDate)
            {
                TodayDate = todayDate;
                switch (TodayDate.DayOfWeek)
                {
                    case DayOfWeek.Sunday:
                        {
                            WeekStart = TodayDate.AddDays(-6);
                            WeekEnd = TodayDate;
                            break;
                        }
                    case DayOfWeek.Monday:
                        {
                            WeekStart = TodayDate;
                            WeekEnd = TodayDate.AddDays(6);
                            break;
                        }
                    default:
                        {
                            WeekStart = TodayDate.AddDays(-((int)TodayDate.DayOfWeek - 1));
                            WeekEnd = WeekStart.AddDays(6);
                            break;
                        }
                }
                if (TodayDate.Month >= 9)
                {
                    SchoolyearStart = new DateOnly(TodayDate.Year, 9, 1);
                    SchoolyearEnd = new DateOnly(TodayDate.Year + 1, 7, 30);
                }
                else
                {
                    SchoolyearStart = new DateOnly(TodayDate.Year - 1, 9, 1);
                    SchoolyearEnd = new DateOnly(TodayDate.Year, 7, 30);
                }
            }

            public string GetWeekSpan()
            { return $"{WeekStart:dd.MM.yyyy}  -  {WeekEnd:dd.MM.yyyy}"; }
            public string GetSchoolyearSpan()
            { return $"{SchoolyearStart.Year}-{SchoolyearEnd.Year}"; }
        }



        [ReplyMenuHandler("/menu")]
        public static async Task ShowMainMenu(ITelegramBotClient botClient, Update update)
        {
            var menuContent = new List<KeyboardButton>()
            {
                new("Назначить занятие ⤵"),
                new("Расписание на неделю 📆")
            };
            var menu = MenuGenerator.ReplyKeyboard(1, menuContent);

            await Helpers.Message.Send(botClient, update,
                msg: "Чем могу быть полезен?",
                option: new OptionMessage() { MenuReplyKeyboardMarkup = menu });
        }
    }
}
