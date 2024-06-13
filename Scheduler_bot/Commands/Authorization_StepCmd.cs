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
        [SlashHandler("/start")]
        public static async Task StepZero(ITelegramBotClient botClient, Update update)  // Запросить тел. номер
        {
            try
            {
                if (update.HasStepHandler())
                    update.ClearStepUserHandler();

                Employee? loggingUser = Dispatcher.DbContext.Employees.FirstOrDefault(c => c.TgBotChatId == update.GetChatId());
                if (loggingUser == null)
                {
                    update.RegisterStepHandler(new StepTelegram(StepOne, new AuthStepCache()));

                    var menuContent = new List<KeyboardButton>() { KeyboardButton.WithRequestContact("Поделиться номером") };
                    var menu = MenuGenerator.ReplyKeyboard(1, menuContent);

                    await Helpers.Message.Send(botClient, update,
                        msg: "Привет! Я помогу вам с составлением расписания!" +
                             "\nДля авторизации мне нужен ваш номер телефона",
                        option: new() { MenuReplyKeyboardMarkup = menu });

                }
                else
                {
                    await Helpers.Message.Send(botClient, update,
                                msg: $"{loggingUser.Name}, вы уже авторизованы 👌" +
                                     "\nМожете продолжать работу");
                    await Dispatcher.ShowMainMenu(botClient, update);
                }
            }
            catch(Exception ex) { await Helpers.Message.Send(botClient, update, msg: ex.Message); }
        }

        public static async Task StepOne(ITelegramBotClient botClient, Update update) // Кэшировать тел.номер. Запросить логин
        {
            try
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

                    if (loggingUser == null)
                    {
                        await Helpers.Message.Send(botClient, update,
                            msg: "Такой номер мне неизвестен(" +
                                 "\nПопробуйте ещё раз");
                    }
                    else
                    {
                        handler!.GetCache<AuthStepCache>().Phone = loggingPhone;

                        handler!.RegisterNextStep(StepTwo);
                        await Helpers.Message.Send(botClient, update,
                            msg: $"Такой номер я знаю!" +
                                 "\nТеперь введите логин",
                            option: new OptionMessage() { ClearMenu = true });
                    }
                }
                else throw new Exception("🚫Ошибка!\nОжидался другой ответ");
            }
            catch (Exception ex) { await Helpers.Message.Send(botClient, update, msg: ex.Message); }
        }

        public static async Task StepTwo(ITelegramBotClient botClient, Update update) // Кэшировать логин. Запросить пароль
        {
            try
            {
                var handler = update.GetStepHandler<StepTelegram>();
                Message? message = update.Message;

                if (message != null && message.Type == MessageType.Text && !string.IsNullOrEmpty(message.Text))
                {
                    string loggingLogin = message.Text;
                    long loggingPhone = handler!.GetCache<AuthStepCache>().Phone;
                    Employee? loggingUser = Dispatcher.DbContext.Employees
                        .FirstOrDefault(c => c.Phone == loggingPhone && c.Login == loggingLogin);

                    if (loggingUser == null)
                    {
                        await Helpers.Message.Send(botClient, update,
                            msg: "Неверный логин(" +
                                 "\nВведите повторно, или начните авторизацию сначала(/start - /login)");
                    }
                    else
                    {
                        handler!.GetCache<AuthStepCache>().Login = loggingLogin;

                        handler!.RegisterNextStep(StepThree);
                        await Helpers.Message.Send(botClient, update,
                            msg: "Теперь пароль");
                    }
                }
                else throw new Exception("🚫Ошибка!\nОжидался другой ответ");
            }
            catch (Exception ex) { await Helpers.Message.Send(botClient, update, msg: ex.Message); }
        }

        public static async Task StepThree(ITelegramBotClient botClient, Update update) // Авторизовать пользователя. Очистить кэш
        {
            try
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

                    if (loggingUser == null)
                    {

                        await Helpers.Message.Send(botClient, update,
                            msg: "Неверный пароль(" +
                                 "\nПопробуте ещё раз, или начните авторизацию сначала(/start - /login)");

                    }
                    else
                    {

                        loggingUser.TgBotChatId = update.GetChatId();
                        Dispatcher.DbContext.SaveChanges(SchedulerDbContext.ChangeLogLevel.Primary, $"Telegram bot confirmed a user - \"{loggingUser.Name}\"");

                        await Helpers.Message.Send(botClient, update,
                            msg: $"Здравствуйте, {loggingUser.Name}!");

                        await Dispatcher.ShowMainMenu(botClient, update);
                        update.ClearStepUserHandler();
                    }
                }
                else throw new Exception("🚫Ошибка!\nОжидался другой ответ");
            }
            catch (Exception ex) { await Helpers.Message.Send(botClient, update, msg: ex.Message); }
        }

        public static async Task BreakStepCmd(ITelegramBotClient botClient, Update update)
        {
            update.ClearStepUserHandler();
            await Helpers.Message.Send(botClient, update,
                        msg: $"Выполнение команды прервано");
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
