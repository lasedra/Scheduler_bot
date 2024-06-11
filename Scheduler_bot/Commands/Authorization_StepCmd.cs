using PRTelegramBot.Attributes;
using PRTelegramBot.Extensions;
using PRTelegramBot.Interface;
using PRTelegramBot.Models;
using PRTelegramBot.Utils;
using Scheduler_bot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Helpers = PRTelegramBot.Helpers;

namespace Scheduler_bot.Commands
{
    public class Authorization_StepCmd
    {
        [SlashHandler("/start", "/login")]
        public static async Task StepZero(ITelegramBotClient botClient, Update update)  // Запросить тел. номер
        {
            Employee? loggingUser = Dispatcher.DbContext.Employees.FirstOrDefault(c => c.TgBotChatId == update.GetChatId());

            if (loggingUser == null)
            {
                update.RegisterStepHandler(new StepTelegram(StepOne, new AuthStepCache()));

                var menuContent = new List<KeyboardButton>() { KeyboardButton.WithRequestContact("Поделиться номером") };
                var menu = MenuGenerator.ReplyKeyboard(1, menuContent);

                await Helpers.Message.Send(botClient, update,
                    msg: "Привет! Я помогу вам с составлением расписания!" +
                         "\nДля авторизации мне нужен ваш номер телефона",
                    option: new OptionMessage() { MenuReplyKeyboardMarkup = menu });

            }
            else
            {
                await Helpers.Message.Send(botClient, update,
                            msg: $"{loggingUser.Name}, вы уже авторизованы." +
                                 "\nМожете продолжать работу");
                await Dispatcher.ShowMainMenu(botClient, update);
            }
        }

        public static async Task StepOne(ITelegramBotClient botClient, Update update) // Кэшировать тел.номер. Запросить логин
        {
            var handler = update.GetStepHandler<StepTelegram>();
            Message? message = update.Message;

            if (message != null && message.Type == MessageType.Contact && message.Contact != null)
            {
                // Cтрогая валидация тел. номера к формату long(80000000000)
                string _phone = message.Contact.PhoneNumber;
                _phone = _phone.Replace(" ", "");
                _phone = _phone.Replace("+", "");
                _phone = _phone.Replace("-", "");
                if (_phone.StartsWith('7'))
                    _phone = ReplaceAt(_phone, 0, '8');

                long loggingPhone = long.Parse(_phone.Trim());
                Employee? loggingUser = Dispatcher.DbContext.Employees
                    .FirstOrDefault(c => c.Phone == loggingPhone);

                if (loggingUser == null){

                    await Helpers.Message.Send(botClient, update,
                        msg: "Такой номер мне неизвестен(" +
                             "\nПопробуйте ещё раз");

                }else{

                    handler!.GetCache<AuthStepCache>().Phone = loggingPhone;

                    handler!.RegisterNextStep(StepTwo);
                    await Helpers.Message.Send(botClient, update,
                        msg: $"Такой номер я знаю!" +
                             "\nТеперь введите логин");
                }
            }
            else
                await Helpers.Message.Send(botClient, update, msg: "Что-то пошло не так...");
        }

        public static async Task StepTwo(ITelegramBotClient botClient, Update update) // Кэшировать логин. Запросить пароль
        {
            var handler = update.GetStepHandler<StepTelegram>();
            Message? message = update.Message;

            if (message != null && message.Type == MessageType.Text && !string.IsNullOrEmpty(message.Text))
            {
                string loggingLogin = message.Text;
                long loggingPhone = handler!.GetCache<AuthStepCache>().Phone;
                Employee? loggingUser = Dispatcher.DbContext.Employees
                    .FirstOrDefault(c => c.Phone == loggingPhone && c.Login == loggingLogin);

                if (loggingUser == null){

                    await Helpers.Message.Send(botClient, update,
                        msg: "Неверный логин(" +
                             "\nПопробуте ещё раз, или начните авторизацию сначала(/start - /login)");

                }else{

                    handler!.GetCache<AuthStepCache>().Login = loggingLogin;

                    handler!.RegisterNextStep(StepThree);
                    await Helpers.Message.Send(botClient, update,
                        msg: "Теперь пароль");
                }
            }
            else
                await Helpers.Message.Send(botClient, update, msg: "Что-то пошло не так...");
        }

        public static async Task StepThree(ITelegramBotClient botClient, Update update) // Авторизовать пользователя. Очистить кэш
        {
            var handler = update.GetStepHandler<StepTelegram>();
            Message? message = update.Message;

            if (message != null && message.Type == MessageType.Text && !string.IsNullOrEmpty(message.Text))
            {
                string loggingPassword = message.Text;
                string loggingLogin = handler!.GetCache<AuthStepCache>().Login;
                long loggingPhone = handler!.GetCache<AuthStepCache>().Phone;
                Employee? loggingUser = Dispatcher.DbContext.Employees
                    .FirstOrDefault(c => c.Phone == loggingPhone && c.Login == loggingLogin && c.Password == loggingPassword);

                if (loggingUser == null){

                    await Helpers.Message.Send(botClient, update,
                        msg: "Неверный пароль(" +
                             "\nПопробуте ещё раз, или начните авторизацию сначала(/start - /login)");

                }else{

                    loggingUser.TgBotChatId = update.GetChatId();
                    Dispatcher.DbContext.SaveChanges();

                    await Helpers.Message.Send(botClient, update,
                        msg: $"Здравствуйте, {loggingUser.Name}!");

                    await Dispatcher.ShowMainMenu(botClient, update);
                    update.ClearStepUserHandler();
                }
            }
            else
                await Helpers.Message.Send(botClient, update, msg: "Что-то пошло не так...");
        }



        public class AuthStepCache : ITelegramCache
        {
            public long Phone { get; set; }
            public string Login { get; set; }
            public bool ClearData()
            {
                this.Phone = 0;
                this.Login = string.Empty;
                return true;
            }
        }
        public static string ReplaceAt(string input, int index, char newChar)
        {
            char[] chars = input.ToCharArray();
            chars[index] = newChar;
            return new string(chars);
        }
    }
}
