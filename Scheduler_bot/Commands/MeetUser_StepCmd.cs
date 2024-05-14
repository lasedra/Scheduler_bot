using PRTelegramBot.Attributes;
using PRTelegramBot.Extensions;
using PRTelegramBot.Models;
using PRTelegramBot.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Helpers = PRTelegramBot.Helpers;

namespace Scheduler_bot.Commands
{
    public class MeetUser_StepCmd // Авторизация. Кэш пользователя хранится до перезагрузки бота
    {
        [SlashHandler("/start")]
        public static async Task StepZero(ITelegramBotClient botClient, Update update)
        {
            if (Dispatcher.CurrentUser.TelegramConfirmed)
            {
                await Helpers.Message.Send(botClient, update,
                        msg: $"Мы уже знакомы, {Dispatcher.CurrentUser.Name})" +
                             "\nМожешь продолжать работу");
                await Dispatcher.ShowMainMenu(botClient, update);
            }
            else
            {
                update.RegisterStepHandler(new StepTelegram(StepOne));

                var menuContent = new List<KeyboardButton>() { KeyboardButton.WithRequestContact("Поделиться номером") };
                var menu = MenuGenerator.ReplyKeyboard(1, menuContent);

                await Helpers.Message.Send(botClient, update,
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
                var theUser = Dispatcher.DbContext.Employees.FirstOrDefault(c => c.PhoneNumber == userPhone);

                if (theUser != null)
                {
                    handler!.RegisterNextStep(StepTwo);
                    Dispatcher.CurrentUser.PhoneNumber = theUser.PhoneNumber;
                    await Helpers.Message.Send(botClient, update,
                        msg: $"Отлично!" +
                             "\nТеперь введи логин");
                }
                else
                {
                    await Helpers.Message.Send(botClient, update,
                        msg: "Такой номер мне не извествен(" +
                             "\nПопробуй ещё раз");
                }
            }
            else
                await Helpers.Message.Send(botClient, update, msg: "Что-то пошло не так...");
        }

        public static async Task StepTwo(ITelegramBotClient botClient, Update update)
        {
            var handler = update.GetStepHandler<StepTelegram>();
            Message? message = update.Message;

            if (message != null && message.Type == MessageType.Text && !string.IsNullOrEmpty(message.Text))
            {
                string userLogin = message.Text;
                var theUser = Dispatcher.DbContext.Employees
                    .FirstOrDefault(c => c.Login == userLogin && c.PhoneNumber == Dispatcher.CurrentUser.PhoneNumber);

                if (theUser != null)
                {
                    handler!.RegisterNextStep(StepThree);
                    Dispatcher.CurrentUser.Login = theUser.Login;
                    await Helpers.Message.Send(botClient, update,
                        msg: "Теперь пароль");
                }
                else
                {
                    await Helpers.Message.Send(botClient, update,
                        msg: "Неверный логин(" +
                             "\nПопробуй заново, или начни сначала, прописав /start");
                }
            }
            else
                await Helpers.Message.Send(botClient, update, msg: "Что-то пошло не так...");

        }

        public static async Task StepThree(ITelegramBotClient botClient, Update update)
        {
            var handler = update.GetStepHandler<StepTelegram>();
            Message? message = update.Message;

            if (message != null && message.Type == MessageType.Text && !string.IsNullOrEmpty(message.Text))
            {
                string userPassword = message.Text;
                var theUser = Dispatcher.DbContext.Employees
                    .FirstOrDefault(c => c.Password == userPassword && c.Login == Dispatcher.CurrentUser.Login && c.PhoneNumber == Dispatcher.CurrentUser.PhoneNumber);

                if (theUser != null)
                {
                    theUser.TelegramConfirmed = true;
                    Dispatcher.DbContext.SaveChanges();
                    Dispatcher.CurrentUser.SetUser(theUser);

                    update.ClearStepUserHandler();
                    await Helpers.Message.Send(botClient, update,
                        msg: $"Здравствуй, уважаемый {Dispatcher.CurrentUser.GetRoleString()} - {theUser.Name}");
                    await Dispatcher.ShowMainMenu(botClient, update);
                }
                else
                {
                    await Helpers.Message.Send(botClient, update,
                        msg: "Неверный пароль(" +
                             "\nПопробуй заново, или начни сначала, прописав /start");
                }
            }
            else
                await Helpers.Message.Send(botClient, update, msg: "Что-то пошло не так...");
        }
    }
}
