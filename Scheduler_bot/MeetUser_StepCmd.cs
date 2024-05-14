using Microsoft.Extensions.Configuration;
using PRTelegramBot.Attributes;
using PRTelegramBot.Extensions;
using PRTelegramBot.Models;
using PRTelegramBot.Utils;
using Scheduler_bot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Scheduler_bot
{
    public class MeetUser_StepCmd // Авторизация. Кэш пользователя хранится до перезагрузки бота
    {
        static IConfiguration AppConfig = new ConfigurationBuilder().AddJsonFile("appconfig.json", optional: false, reloadOnChange: true).Build();
        static SchedulerDbContext DbContext = new(AppConfig.GetConnectionString("localhost"));

        [SlashHandler("/start")]
        public static async Task StepZero(ITelegramBotClient botClient, Update update)
        {
            var handler = update.GetStepHandler<StepTelegram>();

            if (handler != null && handler!.GetCache<UserCache>().TelegramConfirmed)
            {
                await PRTelegramBot.Helpers.Message.Send(botClient, update,
                        msg: $"Мы уже знакомы, {handler!.GetCache<UserCache>().Name})" +
                             "\nМожешь продолжать работу");
                await MainMenu_RplyKybrd.ShowMainMenu(botClient, update);
            }
            else
            {
                update.RegisterStepHandler(new StepTelegram(StepOne, new UserCache()));

                var menuContent = new List<KeyboardButton>() { KeyboardButton.WithRequestContact("Поделиться номером") };
                var menu = MenuGenerator.ReplyKeyboard(1, menuContent);

                await PRTelegramBot.Helpers.Message.Send(botClient, update,
                    msg: "Привет! Я - SchedulerBot. Я помогу тебе с составлением расписания" +
                         "\nМне нужен твой номер телефона, чтобы знать кто-ты",
                    option: new OptionMessage() { MenuReplyKeyboardMarkup = menu });
            }
        }

        public static async Task StepOne(ITelegramBotClient botClient, Update update)
        {
            var handler = update.GetStepHandler<StepTelegram>();
            Message? message = update.Message;

            if (message != null && message.Type == MessageType.Contact && message.Contact != null)
            {
                string userPhone = message.Contact.PhoneNumber
                    .Replace('+', ' ')
                    .Replace('(', ' ')
                    .Replace(')', ' ').Trim();
                var theUser = DbContext.Employees.FirstOrDefault(c => c.PhoneNumber == userPhone);

                if(theUser != null)
                {
                    handler!.RegisterNextStep(StepTwo);
                    handler!.GetCache<UserCache>().PhoneNumber = theUser.PhoneNumber;
                    await PRTelegramBot.Helpers.Message.Send(botClient, update,
                        msg: $"Отлично!" +
                             "\nТеперь введи логин");
                }
                else
                {
                    await PRTelegramBot.Helpers.Message.Send(botClient, update,
                        msg: "Такой номер мне не извествен(" +
                             "\nПопробуй ещё раз");
                }
            }
            else
                await PRTelegramBot.Helpers.Message.Send(botClient, update, msg: "Что-то пошло не так...");
        }

        public static async Task StepTwo(ITelegramBotClient botClient, Update update)
        {
            var handler = update.GetStepHandler<StepTelegram>();
            Message? message = update.Message;

            if (message != null && message.Type == MessageType.Text && !string.IsNullOrEmpty(message.Text))
            {
                string userLogin = message.Text;
                var theUser = DbContext.Employees
                    .FirstOrDefault(c => c.Login == userLogin && c.PhoneNumber == handler!.GetCache<UserCache>().PhoneNumber);
                
                if(theUser != null)
                {
                    handler!.RegisterNextStep(StepThree);
                    handler!.GetCache<UserCache>().Login = theUser.Login;
                    await PRTelegramBot.Helpers.Message.Send(botClient, update,
                        msg: "Теперь пароль");
                }
                else
                {
                    await PRTelegramBot.Helpers.Message.Send(botClient, update,
                        msg: "Неверный логин(" +
                             "\nПопробуй заново, или начни сначала, прописав /start");
                }
            }
            else
                await PRTelegramBot.Helpers.Message.Send(botClient, update, msg: "Что-то пошло не так...");
            
        }

        public static async Task StepThree(ITelegramBotClient botClient, Update update)
        {
            var handler = update.GetStepHandler<StepTelegram>();
            Message? message = update.Message;

            if (message != null && message.Type == MessageType.Text && !string.IsNullOrEmpty(message.Text))
            {
                string userPassword = message.Text;
                var theUser = DbContext.Employees
                    .FirstOrDefault(c => c.Password == userPassword && c.Login == handler!.GetCache<UserCache>().Login && c.PhoneNumber == handler!.GetCache<UserCache>().PhoneNumber);

                if (theUser != null)
                {
                    theUser.TelegramConfirmed = true;
                    DbContext.SaveChanges();
                    handler!.GetCache<UserCache>().SetUser(theUser);

                    await PRTelegramBot.Helpers.Message.Send(botClient, update,
                        msg: $"Здравствуй, уважаемый {handler!.GetCache<UserCache>().GetRoleString()} - {theUser.Name}");
                    await MainMenu_RplyKybrd.ShowMainMenu(botClient, update);
                }
                else
                {
                    await PRTelegramBot.Helpers.Message.Send(botClient, update,
                        msg: "Неверный пароль(" +
                             "\nПопробуй заново, или начни сначала, прописав /start");
                }
            }
            else
                await PRTelegramBot.Helpers.Message.Send(botClient, update, msg: "Что-то пошло не так...");
        }
    }
}
