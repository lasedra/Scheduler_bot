using Microsoft.Extensions.Configuration;
using PRTelegramBot.Attributes;
using PRTelegramBot.Extensions;
using PRTelegramBot.Models;
using Scheduler_bot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Scheduler_bot
{
    public class MeetUser_StepCmd
    {
        static IConfiguration AppConfig = new ConfigurationBuilder().AddJsonFile("appconfig.json", optional: false, reloadOnChange: true).Build();
        static SchedulerDbContext DbContext = new(AppConfig.GetConnectionString("localhost"));

        [SlashHandler("/start")]
        public static async Task StepZero(ITelegramBotClient botClient, Update update)
        {
            update.RegisterStepHandler(new StepTelegram(StepOne, new UserCache()));

            string msg = "Привет! Я - SchedulerBot. Я помогу тебе с составлением расписания" +
                         "\nДля начала введи свой логин:";

            await PRTelegramBot.Helpers.Message.Send(botClient, update, msg);
        }

        public static async Task StepOne(ITelegramBotClient botClient, Update update)
        {
            var handler = update.GetStepHandler<StepTelegram>();
            handler!.GetCache<UserCache>().Login = update.Message.Text;

            string msg = "Теперь пароль:";

            handler.RegisterNextStep(StepTwo, DateTime.Now.AddMinutes(5));
            await PRTelegramBot.Helpers.Message.Send(botClient, update, msg);
        }

        public static async Task StepTwo(ITelegramBotClient botClient, Update update)
        {
            var handler = update.GetStepHandler<StepTelegram>();
            handler!.GetCache<UserCache>().Password = update.Message.Text;
            string userPassword = handler!.GetCache<UserCache>().Password;
            string userLogin = handler!.GetCache<UserCache>().Login;

            string msg = "Результат:";

            var sender = DbContext.Employees.FirstOrDefault(c => c.Password == userPassword && c.Login == userLogin);
            if (sender != null)
            {
                await PRTelegramBot.Helpers.Message.Send(botClient, update, msg + "\nНаш слон!");
                update.ClearStepUserHandler();
            }
            else
            {
                await PRTelegramBot.Helpers.Message.Send(botClient, update, msg + "\nЛиквидирован...");
                update.ClearStepUserHandler();
            }
        }


        [ReplyMenuHandler("ignorestep")]
        public static async Task IngoreStep(ITelegramBotClient botClient, Update update)
        {
            string msg = update.HasStepHandler()
                ? "Следующий шаг проигнорирован"
            : "Следующий шаг отсутствовал";

            await PRTelegramBot.Helpers.Message.Send(botClient, update, msg);
        }
    }
}
