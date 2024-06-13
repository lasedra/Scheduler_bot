using PRTelegramBot.Attributes;
using PRTelegramBot.Models;
using PRTelegramBot.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Helpers = PRTelegramBot.Helpers;
using PRTelegramBot.Helpers;
using PRTelegramBot.Models.Enums;
using PRTelegramBot.Models.InlineButtons;
using PRTelegramBot.Models.TCommands;
using Scheduler_bot.Models;
using PRTelegramBot.Extensions;
using Telegram.Bot.Types.ReplyMarkups;
using PRTelegramBot.Interface;
using Telegram.Bot.Types.Enums;
using System.Globalization;

namespace Scheduler_bot.Commands
{
    public class GetSchedule_PageCmd
    {
        private class GetSchedulePageCache : ITelegramCache
        {
            public List<string> DayTabsPages { get; set; }

            public bool ClearData()
            {
                DayTabsPages = new List<string>();
                return true;
            }
        }

        [InlineCommand]
        public enum CustomTHeader
        {
            SchedulePage = 500
        }

        public class ScheduleController
        {
            public class DayTab
            {
                public DateOnly OfDate { get; set; }
                public DayOfWeek DayOfWeek { get; set; }
                public string StudentGroupCode { get; set; } = null!;
                public string TimingName { get; set; } = null!;
                public int ClassNumber { get; set; }
                public TimeOnly StartTime { get; set; }
                public TimeOnly EndTime { get; set; }
                public string TimeSlot { get { return $"{StartTime}-{EndTime}"; } }
                public Subject? Subject { get; set; }
                public Cabinet? AtCabinet { get; set; }
                public Employee? Tutor { get; set; }
            }

            public Dispatcher.TimePeriod CurrentWeek { get; private set; } = null!;
            public string CurrentGroupCode { get; private set; } = null!;

            public List<DayTab> MondayTab { get; set; } = null!;
            public List<DayTab> TuesdayTab { get; set; } = null!;
            public List<DayTab> WednesdayTab { get; set; } = null!;
            public List<DayTab> ThursdayTab { get; set; } = null!;
            public List<DayTab> FridayTab { get; set; } = null!;
            public List<DayTab>? SaturdayTab { get; set; }


            public ScheduleController(string studentGroupCode)
            {
                SetCurrentWeek(new Dispatcher.TimePeriod(DateOnly.FromDateTime(DateTime.Now)));
                SetCurrentGroupCode(studentGroupCode);
                SetDayTabs();
            }

            public string TransformDayTab(List<DayTab> someTab)
            {
                var culture = new CultureInfo("ru-RU");
                string dayName = culture.DateTimeFormat.GetDayName(someTab.First().DayOfWeek);
                dayName = char.ToUpper(dayName[0]) + dayName.Substring(1);

                string aString = $"{someTab.First().OfDate}, {dayName}";

                foreach (DayTab tab in someTab.OrderBy(c => c.ClassNumber))
                {
                    string tutorName = tab.Tutor != null ? tab.Tutor.Name : "-";
                    string subjectName = tab.Subject != null ? tab.Subject.Name : "-";
                    string cabinetNumber = tab.AtCabinet != null ? tab.AtCabinet.Number : "-";

                    if(tutorName == "-" && subjectName == "-" && cabinetNumber == "-")
                        aString += $"\n{tab.ClassNumber}) -";
                    else
                        aString += $"\n{tab.ClassNumber}){tutorName}, {subjectName}, {cabinetNumber}";
                }

                return aString;
            }

            public List<DayTab> GetCurrentWeekDayTab(string studentGroupCode, DayOfWeek dayOfWeek)
            {
                var query = from dailySchedule in Dispatcher.DbContext.DailyScheduleBodies
                            join classesTiming in Dispatcher.DbContext.ClassesTimingHeaders on dailySchedule.ClassesTimingHeaderId
                                equals classesTiming.ClassesTimingHeaderId into timingGroup
                            from timing in timingGroup.DefaultIfEmpty()
                            join employee in Dispatcher.DbContext.Employees on dailySchedule.EmployeeId
                                equals employee.EmployeeId into employeeGroup
                            from emp in employeeGroup.DefaultIfEmpty()
                            join subject in Dispatcher.DbContext.Subjects on dailySchedule.SubjectId
                                equals subject.SubjectId into subjectGroup
                            from subj in subjectGroup.DefaultIfEmpty()

                            where dailySchedule.OfDate >= CurrentWeek.WeekStart && dailySchedule.OfDate <= CurrentWeek.WeekEnd
                                  &&
                                  dailySchedule.StudentGroupCode == studentGroupCode
                                  &&
                                  dailySchedule.OfDate.DayOfWeek == dayOfWeek
                            select new DayTab
                            {
                                OfDate = dailySchedule.OfDate,
                                DayOfWeek = dailySchedule.OfDate.DayOfWeek,
                                StudentGroupCode = dailySchedule.StudentGroupCode,
                                TimingName = timing.Name,
                                ClassNumber = dailySchedule.ClassNumber,
                                StartTime = Dispatcher.DbContext.ClassesTimingBodies
                                    .First(c => c.ClassesTimingHeader == timing && c.ClassNumber == dailySchedule.ClassNumber).StartTime,
                                EndTime = Dispatcher.DbContext.ClassesTimingBodies
                                    .First(c => c.ClassesTimingHeader == timing && c.ClassNumber == dailySchedule.ClassNumber).EndTime,
                                Tutor = Dispatcher.DbContext.Employees.First(c => c.EmployeeId == dailySchedule.EmployeeId),
                                Subject = Dispatcher.DbContext.Subjects.First(c => c.SubjectId == dailySchedule.SubjectId),
                                AtCabinet = Dispatcher.DbContext.Cabinets.First(c => c.Number == dailySchedule.CabinetNumber)
                            };

                return query.ToList();
            }

            public void SetCurrentWeek(Dispatcher.TimePeriod timePeriod)
            {
                CurrentWeek = timePeriod;
            }

            public void SetCurrentGroupCode(string studentGroupCode)
            {
                CurrentGroupCode = studentGroupCode;
            }

            public void SetDayTabs()
            {
                MondayTab = GetCurrentWeekDayTab(CurrentGroupCode, DayOfWeek.Monday);
                TuesdayTab = GetCurrentWeekDayTab(CurrentGroupCode, DayOfWeek.Tuesday);
                WednesdayTab = GetCurrentWeekDayTab(CurrentGroupCode, DayOfWeek.Wednesday);
                ThursdayTab = GetCurrentWeekDayTab(CurrentGroupCode, DayOfWeek.Thursday);
                FridayTab = GetCurrentWeekDayTab(CurrentGroupCode, DayOfWeek.Friday);
            }

            public List<List<DayTab>> GetDayTabs()
            {
                return new List<List<DayTab>>
            {
                MondayTab,
                TuesdayTab,
                WednesdayTab,
                ThursdayTab,
                FridayTab
            };
            }
        }


        [ReplyMenuHandler("Расписание на неделю 📆")]
        public static async Task StudentGroupChoice(ITelegramBotClient botClient, Update update) // Запросить группу, чьё расписание нужно вывести
        {
            try
            {
                if (update.HasStepHandler())
                    update.ClearStepUserHandler();

                update.RegisterStepHandler(new StepTelegram(ShowSchedulePages, new GetSchedulePageCache()));
                var handler = update.GetStepHandler<StepTelegram>();
                handler!.GetCache<GetSchedulePageCache>().DayTabsPages = new List<string>();

                var groupsList = Dispatcher.DbContext.StudentGroups.ToList();
                var menuContent = new List<KeyboardButton>();
                foreach (var group in groupsList)
                    menuContent.Add(new KeyboardButton(group.StudentGroupCode));
                var menu = MenuGenerator.ReplyKeyboard(groupsList.Count, menuContent);

                await Helpers.Message.Send(botClient, update,
                    msg: "Выберите группу",
                    option: new() { MenuReplyKeyboardMarkup = menu });
            }
            catch (Exception ex) { await Helpers.Message.Send(botClient, update, msg: ex.Message); }
        }


        public static async Task ShowSchedulePages(ITelegramBotClient botClient, Update update)
        {
            try
            {
                if (update.Message != null && update.Message.Type == MessageType.Text && !string.IsNullOrEmpty(update.Message.Text))
                {
                    string _studentGroupCode = update.Message.Text;
                    StudentGroup? currentStudentGroup = Dispatcher.DbContext.StudentGroups.FirstOrDefault(c => c.StudentGroupCode == _studentGroupCode);

                    if (currentStudentGroup == null)
                        throw new Exception("Такой группы нет\nПопробуйте ещё раз");
                    else
                    {
                        var handler = update.GetStepHandler<StepTelegram>();
                        ScheduleController scheduleController = new(currentStudentGroup.StudentGroupCode);

                        var dayTabsRange = scheduleController.GetDayTabs();
                        foreach(var dayTabList in dayTabsRange)
                            handler!.GetCache<GetSchedulePageCache>().DayTabsPages.Add(scheduleController.TransformDayTab(dayTabList));

                        var data = await handler!.GetCache<GetSchedulePageCache>().DayTabsPages.GetPaged(1, 1);
                        var generateMenu = MenuGenerator.GetPageMenu(data.CurrentPage, data.PageCount, CustomTHeader.SchedulePage);

                        // Костыль, чтобы вынудить использовать /menu. Иначе, при повторном проходе, цепочка обрывается на втором шаге
                        await Helpers.Message.Send(botClient, update,
                            msg: $"{currentStudentGroup.StudentGroupCode} - текущее расписание",
                            option: new() { ClearMenu = true });

                        await Helpers.Message.Send(botClient, update,
                            msg: data.Results.FirstOrDefault(),
                            option: new() { MenuInlineKeyboardMarkup = generateMenu });
                    }
                }
                else throw new Exception("🚫Ошибка!\nОжидался другой ответ");
            }
            catch (Exception ex) { await Helpers.Message.Send(botClient, update, msg: ex.Message); }
        }


        [InlineCallbackHandler<THeader>(THeader.NextPage, THeader.PreviousPage, THeader.CurrentPage)]
        public static async Task SchedulePagesHandler(ITelegramBotClient botClient, Update update)
        {
            try
            {
                var handler = update.GetStepHandler<StepTelegram>();
                var command = InlineCallback<PageTCommand>.GetCommandByCallbackOrNull(update.CallbackQuery.Data);
                if (command != null && handler != null)
                {
                    var data = await handler!.GetCache<GetSchedulePageCache>().DayTabsPages.GetPaged(command.Data.Page, 1);
                    var generateMenu = MenuGenerator.GetPageMenu(data.CurrentPage, data.PageCount, CustomTHeader.SchedulePage);
                    await Helpers.Message.Edit(botClient, update,
                        msg: data.Results.FirstOrDefault(),
                        option: new() { MenuInlineKeyboardMarkup = generateMenu });
                }
            }
            catch (Exception ex) { await Helpers.Message.Send(botClient, update, msg: ex.Message); }
        }
    }
}