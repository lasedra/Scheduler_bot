using Microsoft.Extensions.Configuration;
using PRTelegramBot.Models;
using Scheduler_bot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using PRTelegramBot.Attributes;
using Telegram.Bot.Types.ReplyMarkups;
using PRTelegramBot.Utils;

namespace Scheduler_bot
{
    public class Appointment_StepCmd
    {
        static IConfiguration AppConfig = new ConfigurationBuilder().AddJsonFile("appconfig.json", optional: false, reloadOnChange: true).Build();
        static SchedulerDbContext DbContext = new(AppConfig.GetConnectionString("localhost"));

        [ReplyMenuHandler("⤵Назначить занятие")]
        public static async Task ShowMainMenu(ITelegramBotClient botClient, Update update)
        {
            List<StudentGroup> studentGroups = DbContext.StudentGroups.ToList();
            var menuContent = new List<KeyboardButton>();
            foreach(var group in studentGroups)
                menuContent.Add(new KeyboardButton(group.StudentGroupCode));
            var menu = MenuGenerator.ReplyKeyboard(studentGroups.Count, menuContent);
            await PRTelegramBot.Helpers.Message.Send(botClient, update,
                msg: "Выберите группу",
                option: new OptionMessage() { MenuReplyKeyboardMarkup = menu });
        }
    }
}
