using PRTelegramBot.Models;
using Scheduler_bot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using PRTelegramBot.Attributes;
using Telegram.Bot.Types.ReplyMarkups;
using PRTelegramBot.Utils;
using PRTelegramBot.Extensions;
using Helpers = PRTelegramBot.Helpers;

namespace Scheduler_bot.Commands
{
    public class Appointment_StepCmd
    {
        [ReplyMenuHandler("Назначить занятие⤵")]
        public static async Task StepZero(ITelegramBotClient botClient, Update update)
        {
            update.RegisterStepHandler(new StepTelegram(StepOne));

            List<StudentGroup> studentGroups = Dispatcher.DbContext.StudentGroups.ToList();
            var menuContent = new List<KeyboardButton>();
            foreach (var group in studentGroups)
                menuContent.Add(new KeyboardButton(group.StudentGroupCode));
            var menu = MenuGenerator.ReplyKeyboard(studentGroups.Count, menuContent);

            await Helpers.Message.Send(botClient, update,
                msg: "Выберите группу",
                option: new OptionMessage() { MenuReplyKeyboardMarkup = menu });
        }

        public static async Task StepOne(ITelegramBotClient botClient, Update update)
        {
            var handler = update.GetStepHandler<StepTelegram>();
            List<Subject> subjects = Dispatcher.DbContext.Tutions.Where(tution => tution.EmployeeId == Dispatcher.CurrentUser.EmployeeId && tution.EndDate == null)
                                    .Select(tution => tution.Subject)
                                    .Distinct()
                                    .ToList();
            if (subjects.Any())
            {
                var menuContent = new List<KeyboardButton>();
                foreach (var subject in subjects)
                    menuContent.Add(new KeyboardButton(subject.Name));
                var menu = MenuGenerator.ReplyKeyboard(subjects.Count, menuContent);

                handler!.RegisterNextStep(StepTwo);
                await Helpers.Message.Send(botClient, update,
                    msg: "Выберите предмет",
                    option: new OptionMessage() { MenuReplyKeyboardMarkup = menu });
            }
            else
            {
                update.ClearStepUserHandler();
                await Helpers.Message.Send(botClient, update,
                    msg: "Кажется, вы не можете вести ни один предмет(" +
                         "\nЗа доп. информацией обратитесь к менеджеру");
                await Dispatcher.ShowMainMenu(botClient, update);
            }
        }

        public static async Task StepTwo(ITelegramBotClient botClient, Update update)
        {
            var handler = update.GetStepHandler<StepTelegram>();
            List<Cabinet> cabinets = Dispatcher.DbContext.Cabinets.ToList();
            var menuContent = new List<KeyboardButton>();
            foreach (var cabinet in cabinets)
                menuContent.Add(new KeyboardButton($"{cabinet.Number} - {cabinet.Name}"));
            var menu = MenuGenerator.ReplyKeyboard(1, menuContent);

            handler!.RegisterNextStep(StepTwo);
            await Helpers.Message.Send(botClient, update,
                msg: "Выберите кабинет",
                option: new OptionMessage() { MenuReplyKeyboardMarkup = menu });
        }

        public static async Task StepThree(ITelegramBotClient botClient, Update update)
        {
            //var handler = update.GetStepHandler<StepTelegram>();
            //List<DailyScheduleBody> weekDays = Dispatcher.DbContext.DailyScheduleBodies.Where(c => c.Employee == null && c.Subject == null && c.CabinetNumber == null).ToList();
            //var menuContent = new List<KeyboardButton>();
            //foreach (var day in weekDays)
            //    menuContent.Add(new KeyboardButton($"{cabinet.Number} - {cabinet.Name}"));
            //var menu = MenuGenerator.ReplyKeyboard(1, menuContent);

            //handler!.RegisterNextStep(StepTwo);
            //await Helpers.Message.Send(botClient, update,
            //    msg: "Выберите кабинет",
            //    option: new OptionMessage() { MenuReplyKeyboardMarkup = menu });
        }
    }
}
