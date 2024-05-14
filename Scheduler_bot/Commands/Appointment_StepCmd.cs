using PRTelegramBot.Models;
using Scheduler_bot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using PRTelegramBot.Attributes;
using Telegram.Bot.Types.ReplyMarkups;
using PRTelegramBot.Utils;
using PRTelegramBot.Extensions;
using Helpers = PRTelegramBot.Helpers;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Scheduler_bot.Commands
{
    public class Appointment_StepCmd
    {
        private static class Appointment
        {
            public static string studentGroup {  get; set; } = string.Empty;
            public static string subject { get; set; } = string.Empty;
            public static string cabinet { get; set; } = string.Empty;
            public static DayOfWeek dayOfTheWeek { get; set; }

            public static bool ClearData()
            {
                studentGroup = string.Empty;
                subject = string.Empty;
                cabinet = string.Empty;
                dayOfTheWeek = DayOfWeek.Sunday;
                return true;
            }
        }

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
            Appointment.studentGroup = update.Message.Text;

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
                await Dispatcher.ShowMainMenu(botClient, update);
                Appointment.ClearData();

                await Helpers.Message.Send(botClient, update,
                    msg: "Кажется, вы не можете вести ни один предмет(" +
                         "\nЗа доп. информацией обратитесь к менеджеру");
            }
        }

        public static async Task StepTwo(ITelegramBotClient botClient, Update update)
        {
            var handler = update.GetStepHandler<StepTelegram>();
            Appointment.subject = update.Message.Text;

            List<Cabinet> cabinets = Dispatcher.DbContext.Cabinets.ToList();
            var menuContent = new List<KeyboardButton>();
            foreach (var cabinet in cabinets)
                menuContent.Add(new KeyboardButton($"{cabinet.Number} - {cabinet.Name}"));
            var menu = MenuGenerator.ReplyKeyboard(1, menuContent);

            handler!.RegisterNextStep(StepThree);
            await Helpers.Message.Send(botClient, update,
                msg: "Выберите кабинет",
                option: new OptionMessage() { MenuReplyKeyboardMarkup = menu });
        }

        public static async Task StepThree(ITelegramBotClient botClient, Update update)
        {
            var handler = update.GetStepHandler<StepTelegram>();
            Appointment.cabinet = update.Message.Text;

            Dispatcher.TimePeriod currentWeek = new(DateOnly.FromDateTime(DateTime.Today));
            var openDays = Dispatcher.DbContext.DailyScheduleBodies
                .Where(c => c.StudentGroupCode == Appointment.studentGroup && c.Employee == null && c.Subject == null && c.CabinetNumber == null && 
                            c.OfDate >= currentWeek.WeekStart && c.OfDate <= currentWeek.WeekEnd)
                .GroupBy(c => c.OfDate.DayOfWeek).Select(group => group.Key)
                .ToList();
            var culture = new CultureInfo("ru-RU");

            var menuContent = new List<KeyboardButton>();
            foreach (var day in openDays)
                menuContent.Add(new KeyboardButton(culture.DateTimeFormat.GetAbbreviatedDayName(day)));
            //menuContent.Add(new KeyboardButton(culture.DateTimeFormat.GetDayName(day)));
            var menu = MenuGenerator.ReplyKeyboard(openDays.Count, menuContent);

            handler!.RegisterNextStep(StepFour);
            await Helpers.Message.Send(botClient, update,
                msg: "Выберите день недели",
                option: new OptionMessage() { MenuReplyKeyboardMarkup = menu });
        }

        public static async Task StepFour(ITelegramBotClient botClient, Update update)
        {
            var handler = update.GetStepHandler<StepTelegram>();
            Appointment.dayOfTheWeek = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), update.Message.Text.ToString(), true);

            var openClasses = Dispatcher.DbContext.DailyScheduleBodies
                .Where(c => c.StudentGroupCode == Appointment.studentGroup && c.OfDate.DayOfWeek == Appointment.dayOfTheWeek);

            var menuContent = new List<KeyboardButton>();
            foreach (var lesson in openClasses)
                menuContent.Add(new KeyboardButton(lesson.ClassNumber.ToString()));
            var menu = MenuGenerator.ReplyKeyboard(openClasses.Count(), menuContent);

            //handler!.RegisterNextStep(StepFour);
            await Helpers.Message.Send(botClient, update,
                msg: "Выберите пары",
                option: new OptionMessage() { MenuReplyKeyboardMarkup = menu });
        }
    }
}
