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

namespace Scheduler_bot.Commands
{
    public class Appointment_StepCmd
    {
        private static class Appointment
        {
            public static string studentGroupCode {  get; set; } = string.Empty;
            public static Subject subject { get; set; } = null!;
            public static Cabinet cabinet { get; set; } = null!;
            public static DayOfWeek dayOfTheWeek { get; set; }
            public static int classNumber { get; set; } = 0;

            public static bool ClearData()
            {
                studentGroupCode = string.Empty;
                subject =  null!;
                cabinet = null!;
                dayOfTheWeek = DayOfWeek.Sunday;
                classNumber = 0;
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
        } //Группа

        public static async Task StepOne(ITelegramBotClient botClient, Update update)
        {
            var handler = update.GetStepHandler<StepTelegram>();
            Appointment.studentGroupCode = update.Message.Text;

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
        } //Предмет

        public static async Task StepTwo(ITelegramBotClient botClient, Update update)
        {
            var handler = update.GetStepHandler<StepTelegram>();
            Appointment.subject = Dispatcher.DbContext.Subjects.First(c => c.Name == update.Message.Text);

            List<Cabinet> cabinets = Dispatcher.DbContext.Cabinets.ToList();
            var menuContent = new List<KeyboardButton>();
            foreach (var cabinet in cabinets)
                menuContent.Add(new KeyboardButton($"{cabinet.Number}"));
            //menuContent.Add(new KeyboardButton($"{cabinet.Number} - {cabinet.Name}"));
            var menu = MenuGenerator.ReplyKeyboard(1, menuContent);

            handler!.RegisterNextStep(StepThree);
            await Helpers.Message.Send(botClient, update,
                msg: "Выберите кабинет",
                option: new OptionMessage() { MenuReplyKeyboardMarkup = menu });
        } //Кабинет

        public static async Task StepThree(ITelegramBotClient botClient, Update update)
        {
            var handler = update.GetStepHandler<StepTelegram>();
            Appointment.cabinet = Dispatcher.DbContext.Cabinets.First(c => c.Number == update.Message.Text);

            Dispatcher.TimePeriod currentWeek = new(DateOnly.FromDateTime(DateTime.Today));
            var openDays = Dispatcher.DbContext.DailyScheduleBodies
                .Where(c => c.StudentGroupCode == Appointment.studentGroupCode && c.Employee == null && c.Subject == null && c.CabinetNumber == null && 
                            c.OfDate >= currentWeek.WeekStart && c.OfDate <= currentWeek.WeekEnd)
                .GroupBy(c => c.OfDate.DayOfWeek).Select(group => group.Key)
                .ToList();
            var culture = new CultureInfo("ru-RU");

            var menuContent = new List<KeyboardButton>();
            foreach (var day in openDays)
                menuContent.Add(new KeyboardButton(culture.DateTimeFormat.GetAbbreviatedDayName(day)));
            var menu = MenuGenerator.ReplyKeyboard(openDays.Count, menuContent);

            handler!.RegisterNextStep(StepFour);
            await Helpers.Message.Send(botClient, update,
                msg: "Выберите день недели",
                option: new OptionMessage() { MenuReplyKeyboardMarkup = menu });
        } //День недели

        public static async Task StepFour(ITelegramBotClient botClient, Update update)
        {
            var handler = update.GetStepHandler<StepTelegram>();
            switch (update.Message.Text)
            {
                case "пн":
                    Appointment.dayOfTheWeek = DayOfWeek.Monday;
                    break;
                case "вт":
                    Appointment.dayOfTheWeek = DayOfWeek.Tuesday;
                    break;
                case "ср":
                    Appointment.dayOfTheWeek = DayOfWeek.Wednesday;
                    break;
                case "чт":
                    Appointment.dayOfTheWeek = DayOfWeek.Thursday;
                    break;
                case "пт":
                    Appointment.dayOfTheWeek = DayOfWeek.Saturday;
                    break;
            }

            Dispatcher.TimePeriod currentWeek = new(DateOnly.FromDateTime(DateTime.Today));
            var openLessons = Dispatcher.DbContext.DailyScheduleBodies
                .Where(c => c.StudentGroupCode == Appointment.studentGroupCode && c.Employee == null && c.Subject == null && c.CabinetNumber == null &&
                            c.OfDate >= currentWeek.WeekStart && c.OfDate <= currentWeek.WeekEnd &&
                            c.OfDate.DayOfWeek == Appointment.dayOfTheWeek).ToList();

            var menuContent = new List<KeyboardButton>();
            foreach (var lesson in openLessons)
                menuContent.Add(new KeyboardButton(lesson.ClassNumber.ToString()));
            var menu = MenuGenerator.ReplyKeyboard(openLessons.Count(), menuContent);

            handler!.RegisterNextStep(StepFive);
            await Helpers.Message.Send(botClient, update,
                msg: "Выберите пары",
                option: new OptionMessage() { MenuReplyKeyboardMarkup = menu });
        } //Пары

        public static async Task StepFive(ITelegramBotClient botClient, Update update)
        {
            var handler = update.GetStepHandler<StepTelegram>();
            Appointment.classNumber = Convert.ToInt32(update.Message.Text);

            var culture = new CultureInfo("ru-RU");
            Dispatcher.TimePeriod currentWeek = new(DateOnly.FromDateTime(DateTime.Today));
            DailyScheduleBody newAppointment = Dispatcher.DbContext.DailyScheduleBodies
                .First(c => c.StudentGroupCode == Appointment.studentGroupCode && 
                            c.Employee == null && c.Subject == null && c.CabinetNumber == null &&
                            c.OfDate >= currentWeek.WeekStart && c.OfDate <= currentWeek.WeekEnd &&
                            c.OfDate.DayOfWeek == Appointment.dayOfTheWeek &&
                            c.ClassNumber == Appointment.classNumber);

            newAppointment.Employee = Dispatcher.DbContext.Employees.First(c => c.EmployeeId == Dispatcher.CurrentUser.EmployeeId);
            newAppointment.Subject = Dispatcher.DbContext.Subjects.First(c => c.SubjectId == Appointment.subject.SubjectId);
            newAppointment.CabinetNumber = Appointment.cabinet.Number;
            Dispatcher.DbContext.SaveChanges();

            update.ClearStepUserHandler();
            await Helpers.Message.Send(botClient, update,
                msg: "Готово! Вы назначили новое занятие:" +
                $"\n- Дата: {newAppointment.OfDate}, {culture.DateTimeFormat.GetAbbreviatedDayName(newAppointment.OfDate.DayOfWeek)}" +
                $"\n- Пара: {newAppointment.ClassNumber}" +
                $"\n- Предмет: {newAppointment.Subject.Name}" +
                $"\n- Кабинет: {newAppointment.CabinetNumber}");
            await Dispatcher.ShowMainMenu(botClient, update);
        } // Финал
    }
}
