using PRTelegramBot.Models;
using Scheduler_bot.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using PRTelegramBot.Attributes;
using Telegram.Bot.Types.ReplyMarkups;
using PRTelegramBot.Utils;
using PRTelegramBot.Extensions;
using Helpers = PRTelegramBot.Helpers;
using Microsoft.EntityFrameworkCore;
using PRTelegramBot.Interface;
using Telegram.Bot.Types.Enums;
using System.Globalization;

namespace Scheduler_bot.Commands
{
    public class Appointment_StepCmd
    {
        [ReplyMenuHandler("Назначить занятие⤵")]
        public static async Task StepZero(ITelegramBotClient botClient, Update update) // Запросить группу
        {
            Employee currentUser = Dispatcher.DbContext.Employees.First(c => c.TgBotChatId == update.GetChatId());

            List<Subject> tutionSubjects = Dispatcher.DbContext.Tutions
                                                .Where(tution => tution.EmployeeId == currentUser.EmployeeId && tution.EndDate == null)
                                                .Select(tution => tution.Subject)
                                                .Distinct()
                                                .ToList();
            List<Subject> studyingSubjects = Dispatcher.DbContext.Studyings
                                                .Select(studying => studying.Subject)
                                                .Distinct()
                                                .ToList();
            List<Subject> commonSubjects = tutionSubjects
                                                .Intersect(studyingSubjects)
                                                .ToList();
            //List<StudentGroup> allowedGroups = Dispatcher.DbContext.StudentGroups
            //                                    .Include(c => c.Studyings)
            //                                    .Where(group => group.Studyings.Any(studying => commonSubjects.Any(subject => studying.SubjectId == subject.SubjectId)))
            //                                    .Distinct()
            //                                    .ToList();
            List<StudentGroup> allowedGroups = new();
            foreach (StudentGroup group in Dispatcher.DbContext.StudentGroups.Include(c => c.Studyings))
                foreach (Subject subject in commonSubjects)
                    if (group.Studyings.FirstOrDefault(c => c.SubjectId == subject.SubjectId) != null)
                        allowedGroups.Add(group);
            allowedGroups = allowedGroups.Distinct().ToList();

            if (allowedGroups.Count == 0){

                await Helpers.Message.Send(botClient, update,
                    msg: "К сожалению, вы не можете вести предметы ни у одной группы(");
                await Dispatcher.ShowMainMenu(botClient, update);

            }else{

                update.RegisterStepHandler(new StepTelegram(StepOne, new AppointmentStepCache()));
                var handler = update.GetStepHandler<StepTelegram>();
                handler!.GetCache<AppointmentStepCache>().TutionSubjects = tutionSubjects;
                handler!.GetCache<AppointmentStepCache>().StudyingSubjects = studyingSubjects;
                handler!.GetCache<AppointmentStepCache>().CommonSubjects = commonSubjects;
                handler!.GetCache<AppointmentStepCache>().AllowedGroups = allowedGroups;

                var menuContent = new List<KeyboardButton>();
                foreach (var group in allowedGroups)
                    menuContent.Add(new KeyboardButton(group.StudentGroupCode));
                var menu = MenuGenerator.ReplyKeyboard(allowedGroups.Count, menuContent);

                await Helpers.Message.Send(botClient, update,
                    msg: "Выберите группу",
                    option: new OptionMessage() { MenuReplyKeyboardMarkup = menu });

            }
        }

        public static async Task StepOne(ITelegramBotClient botClient, Update update) // Кэшировать группу. Запросить предмет
        {
            Employee currentUser = Dispatcher.DbContext.Employees.First(c => c.TgBotChatId == update.GetChatId());
            var handler = update.GetStepHandler<StepTelegram>();
            Message? message = update.Message;

            if (message != null && message.Type == MessageType.Text && !string.IsNullOrEmpty(message.Text))
            {
                string _studentGroupCode = message.Text;
                StudentGroup? currentStudentGroup = Dispatcher.DbContext.StudentGroups.FirstOrDefault(c => c.StudentGroupCode == _studentGroupCode);

                if(currentStudentGroup == null){

                    await Helpers.Message.Send(botClient, update,
                        msg: "Такой группы нет(" +
                             "\nПопробуйте ещё раз");

                }else{

                    handler!.RegisterNextStep(StepTwo);
                    handler!.GetCache<AppointmentStepCache>().StudentGroupCode = currentStudentGroup.StudentGroupCode;

                    // TODO: выборка не включает выбранную группу, а берёт общие. Исправить
                    List<Subject> _commonSubjects = handler!.GetCache<AppointmentStepCache>().CommonSubjects;
                    var menuContent = new List<KeyboardButton>();
                    foreach (var subject in _commonSubjects)
                        menuContent.Add(new KeyboardButton(subject.Name));
                    var menu = MenuGenerator.ReplyKeyboard(_commonSubjects.Count, menuContent);

                    await Helpers.Message.Send(botClient, update,
                        msg: "Выберите предмет",
                        option: new OptionMessage() { MenuReplyKeyboardMarkup = menu });

                }
            }
            else
                await Helpers.Message.Send(botClient, update, msg: "Что-то пошло не так...");
        }

        public static async Task StepTwo(ITelegramBotClient botClient, Update update) // Кэшировать предмет. Запросить кабинет
        {
            Employee currentUser = Dispatcher.DbContext.Employees.First(c => c.TgBotChatId == update.GetChatId());
            var handler = update.GetStepHandler<StepTelegram>();
            Message? message = update.Message;

            if (message != null && message.Type == MessageType.Text && !string.IsNullOrEmpty(message.Text))
            {
                string _subjectName = message.Text;
                Subject? currentSubject = Dispatcher.DbContext.Subjects.FirstOrDefault(c => c.Name == _subjectName);

                if(currentSubject == null){

                    await Helpers.Message.Send(botClient, update,
                        msg: "Такого предмета нет(" +
                             "\nПопробуйте ещё раз");

                }else{

                    handler!.RegisterNextStep(StepThree);
                    handler!.GetCache<AppointmentStepCache>().Subject = currentSubject;

                    List<Cabinet> cabinets = Dispatcher.DbContext.Cabinets.ToList();
                    var menuContent = new List<KeyboardButton>();
                    foreach (var cabinet in cabinets)
                        menuContent.Add(new KeyboardButton(cabinet.Number));
                    var menu = MenuGenerator.ReplyKeyboard(cabinets.Count, menuContent);

                    await Helpers.Message.Send(botClient, update,
                        msg: "Выберите кабинет",
                        option: new OptionMessage() { MenuReplyKeyboardMarkup = menu });

                }
            }
            else
                await Helpers.Message.Send(botClient, update, msg: "Что-то пошло не так...");
        }

        public static async Task StepThree(ITelegramBotClient botClient, Update update) // Кэшировать кабинет. Запросить день недели
        {
            Employee currentUser = Dispatcher.DbContext.Employees.First(c => c.TgBotChatId == update.GetChatId());
            var handler = update.GetStepHandler<StepTelegram>();
            Message? message = update.Message;

            if (message != null && message.Type == MessageType.Text && !string.IsNullOrEmpty(message.Text))
            {
                string _cabinetNumber = message.Text;
                Cabinet? currentCabinet = Dispatcher.DbContext.Cabinets.FirstOrDefault(c => c.Number == _cabinetNumber);

                if (currentCabinet == null){

                    await Helpers.Message.Send(botClient, update,
                        msg: "Такого кабинета нет(" +
                             "\nПопробуйте ещё раз");

                }
                else{

                    //handler!.RegisterNextStep(StepFour);
                    handler!.GetCache<AppointmentStepCache>().Cabinet = currentCabinet;

                    Dispatcher.TimePeriod currentWeek = new(DateOnly.FromDateTime(DateTime.Today));
                    var openDays = Dispatcher.DbContext.DailyScheduleBodies.Where(c => c.StudentGroupCode == handler!.GetCache<AppointmentStepCache>().StudentGroupCode && 
                                                                                       c.Employee == null && 
                                                                                       c.Subject == null && 
                                                                                       c.CabinetNumber == null &&
                                                                                       c.OfDate >= currentWeek.WeekStart && 
                                                                                       c.OfDate <= currentWeek.WeekEnd)
                                                                           .GroupBy(c => c.OfDate.DayOfWeek)
                                                                           .Select(group => group.Key)
                                                                           .ToList();
                    var culture = new CultureInfo("ru-RU");

                    var menuContent = new List<KeyboardButton>();
                    foreach (var day in openDays)
                        menuContent.Add(new KeyboardButton(culture.DateTimeFormat.GetAbbreviatedDayName(day)));
                    var menu = MenuGenerator.ReplyKeyboard(openDays.Count, menuContent);

                    await Helpers.Message.Send(botClient, update,
                        msg: "Выберите день недели",
                        option: new OptionMessage() { MenuReplyKeyboardMarkup = menu });

                }
            }
            else
                await Helpers.Message.Send(botClient, update, msg: "Что-то пошло не так...");
        }

        //public static async Task StepFour(ITelegramBotClient botClient, Update update)
        //{
        //    var handler = update.GetStepHandler<StepTelegram>();
        //    switch (update.Message.Text)
        //    {
        //        case "пн":
        //            Appointment.dayOfTheWeek = DayOfWeek.Monday;
        //            break;
        //        case "вт":
        //            Appointment.dayOfTheWeek = DayOfWeek.Tuesday;
        //            break;
        //        case "ср":
        //            Appointment.dayOfTheWeek = DayOfWeek.Wednesday;
        //            break;
        //        case "чт":
        //            Appointment.dayOfTheWeek = DayOfWeek.Thursday;
        //            break;
        //        case "пт":
        //            Appointment.dayOfTheWeek = DayOfWeek.Saturday;
        //            break;
        //    }

        //    Dispatcher.TimePeriod currentWeek = new(DateOnly.FromDateTime(DateTime.Today));
        //    var openLessons = Dispatcher.DbContext.DailyScheduleBodies
        //        .Where(c => c.StudentGroupCode == Appointment.studentGroupCode && c.Employee == null && c.Subject == null && c.CabinetNumber == null &&
        //                    c.OfDate >= currentWeek.WeekStart && c.OfDate <= currentWeek.WeekEnd &&
        //                    c.OfDate.DayOfWeek == Appointment.dayOfTheWeek).ToList();

        //    var menuContent = new List<KeyboardButton>();
        //    foreach (var lesson in openLessons)
        //        menuContent.Add(new KeyboardButton(lesson.ClassNumber.ToString()));
        //    var menu = MenuGenerator.ReplyKeyboard(openLessons.Count(), menuContent);

        //    handler!.RegisterNextStep(StepFive);
        //    await Helpers.Message.Send(botClient, update,
        //        msg: "Выберите пары",
        //        option: new OptionMessage() { MenuReplyKeyboardMarkup = menu });
        //} //Пары

        //public static async Task StepFive(ITelegramBotClient botClient, Update update)
        //{
        //    Employee currentUser = Dispatcher.TelegramUsers.First(c => c.Value == update.GetChatId()).Key;

        //    var handler = update.GetStepHandler<StepTelegram>();
        //    Appointment.classNumber = Convert.ToInt32(update.Message.Text);

        //    var culture = new CultureInfo("ru-RU");
        //    Dispatcher.TimePeriod currentWeek = new(DateOnly.FromDateTime(DateTime.Today));
        //    DailyScheduleBody newAppointment = Dispatcher.DbContext.DailyScheduleBodies
        //        .First(c => c.StudentGroupCode == Appointment.studentGroupCode &&
        //                    c.Employee == null && c.Subject == null && c.CabinetNumber == null &&
        //                    c.OfDate >= currentWeek.WeekStart && c.OfDate <= currentWeek.WeekEnd &&
        //                    c.OfDate.DayOfWeek == Appointment.dayOfTheWeek &&
        //                    c.ClassNumber == Appointment.classNumber);

        //    newAppointment.Employee = Dispatcher.DbContext.Employees.First(c => c.EmployeeId == currentUser.EmployeeId);
        //    newAppointment.Subject = Dispatcher.DbContext.Subjects.First(c => c.SubjectId == Appointment.subject.SubjectId);
        //    newAppointment.CabinetNumber = Appointment.cabinet.Number;
        //    Dispatcher.DbContext.SaveChanges();

        //    update.ClearStepUserHandler();
        //    await Helpers.Message.Send(botClient, update,
        //        msg: "Готово! Вы назначили новое занятие:" +
        //        $"\n- Дата: {newAppointment.OfDate}, {culture.DateTimeFormat.GetAbbreviatedDayName(newAppointment.OfDate.DayOfWeek)}" +
        //        $"\n- Пара: {newAppointment.ClassNumber}" +
        //        $"\n- Предмет: {newAppointment.Subject.Name}" +
        //        $"\n- Кабинет: {newAppointment.CabinetNumber}");
        //    await Dispatcher.ShowMainMenu(botClient, update);
        //} // Финал



        private class AppointmentStepCache : ITelegramCache
        {
            public List<Subject> TutionSubjects { get; set; }
            public List<Subject> StudyingSubjects { get; set; }
            public List<Subject> CommonSubjects { get; set; }
            public List<StudentGroup> AllowedGroups { get; set; }
            public string StudentGroupCode { get; set; }
            public Subject Subject { get; set; }
            public Cabinet Cabinet { get; set; }
            public DayOfWeek DayOfTheWeek { get; set; }
            public int ClassNumber { get; set; }

            public bool ClearData()
            {
                TutionSubjects = new List<Subject>();
                StudyingSubjects = new List<Subject>();
                CommonSubjects = new List<Subject>();
                AllowedGroups = new List<StudentGroup>();
                StudentGroupCode = string.Empty;
                Subject = null!;
                Cabinet = null!;
                DayOfTheWeek = DayOfWeek.Sunday;
                ClassNumber = 0;
                return true;
            }
        }
    }
}
