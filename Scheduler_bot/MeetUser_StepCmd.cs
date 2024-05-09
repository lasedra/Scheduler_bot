using Microsoft.Extensions.Configuration;
using PRTelegramBot.Attributes;
using PRTelegramBot.Extensions;
using PRTelegramBot.Models;
using PRTelegramBot.Utils;
using Scheduler_bot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Scheduler_bot
{
    public class MeetUser_StepCmd
    {
        static IConfiguration AppConfig = new ConfigurationBuilder().AddJsonFile("appconfig.json", optional: false, reloadOnChange: true).Build();
        static SchedulerDbContext DbContext = new(AppConfig.GetConnectionString("localhost"));


        [SlashHandler("/login")]
        public static async Task StepZero(ITelegramBotClient botClient, Update update)
        {
            update.ClearCacheData();
            update.RegisterStepHandler(new StepTelegram(StepOne, new UserCache()));

            string msg = "Привет! Я - SchedulerBot. Я помогу тебе с составлением расписания" +
                         "\nДля начала введи свой логин:";

            await PRTelegramBot.Helpers.Message.Send(botClient, update, msg);
        }

        public static async Task StepOne(ITelegramBotClient botClient, Update update)
        {
            var handler = update.GetStepHandler<StepTelegram>();
            handler!.GetCache<UserCache>().Login = update.Message.Text;
            string userLogin = handler!.GetCache<UserCache>().Login;

            var loggingUser = DbContext.Employees.FirstOrDefault(c => c.Login == userLogin);
            if (loggingUser != null)
            {
                handler.RegisterNextStep(StepTwo);
                await PRTelegramBot.Helpers.Message.Send(botClient, update,
                    msg: "Я вас узнал!" +
                         "\nТеперь введите пароль:");
            }
            else
            {
                var menuContent = new List<KeyboardButton>() { new("/login") };
                var menu = MenuGenerator.ReplyKeyboard(1, menuContent);

                await PRTelegramBot.Helpers.Message.Send(botClient, update,
                    msg: "К сожалению, такой учётной записи не существует." +
                         "\nОбратитесь к менеджеру или попробуйте авторизоваться снова.",
                    option: new OptionMessage() { MenuReplyKeyboardMarkup = menu });
                update.ClearStepUserHandler();
            }
        }

        public static async Task StepTwo(ITelegramBotClient botClient, Update update)
        {
            var handler = update.GetStepHandler<StepTelegram>();
            handler!.GetCache<UserCache>().Password = update.Message.Text;
            string userPassword = handler!.GetCache<UserCache>().Password;
            string userLogin = handler!.GetCache<UserCache>().Login;

            var DbUser = DbContext.Employees.FirstOrDefault(c => c.Password == userPassword && c.Login == userLogin);
            if (DbUser != null)
            {
                handler!.GetCache<UserCache>().SetUser(DbUser);

                await PRTelegramBot.Helpers.Message.Send(botClient, update,
                    msg: $"Здравствуйте, вы {handler!.GetCache<UserCache>().GetRoleString()} - {handler!.GetCache<UserCache>().Name}");
                update.ClearStepUserHandler();

                if(DbUser.TelegramId != "@"+UserCache.tempTelegramId)
                {
                    await PRTelegramBot.Helpers.Message.Send(botClient, update,
                    msg: $"Похоже сейчас вы используете другой Telegram аккаунт: @{UserCache.tempTelegramId}" +
                         $"\nЕсли ваш аккаунт {DbUser.TelegramId} больше не актуален, свяжитесь с менеджером для обновления ваших контактов.");
                }
            }
            else
            {
                var menuContent = new List<KeyboardButton>() { new("/login") };
                var menu = MenuGenerator.ReplyKeyboard(1, menuContent);

                await PRTelegramBot.Helpers.Message.Send(botClient, update,
                    msg: "Неправильный пароль(" +
                         "\nПопробуйте ещё раз или авторизуйтесь заново",
                    option: new OptionMessage() { MenuReplyKeyboardMarkup = menu });
            }
        }
    }
}
