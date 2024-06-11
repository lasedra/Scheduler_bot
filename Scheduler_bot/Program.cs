using Microsoft.EntityFrameworkCore;
using PRTelegramBot.Core;
using Scheduler_bot;

PRBot botClient = new(options =>
{
    options.Token = Dispatcher.AppConfig.GetSection("Bot token").Value ?? "NO_BOT_TOKEN";
    options.ClearUpdatesOnStart = true;
});

botClient.OnLogCommon += ConsolePrint_OnLogCommon;
botClient.OnLogError += ConsolePrint_OnLogError;
await botClient.Start();

while (true)
    if (Console.ReadLine()!.ToLower() == "exit")
    {
        await Dispatcher.DbContext.Employees.ForEachAsync(c => c.TgBotChatId = null);
        Dispatcher.DbContext.SaveChanges(Scheduler_bot.Models.SchedulerDbContext.ChangeLogLevel.Primary, "Telegram bot shut down");
        Environment.Exit(0);
    }

static void ConsolePrint_OnLogCommon(string msg, Enum typeEvent, ConsoleColor color)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"{DateTime.Now:g}: {msg}");
    Console.ResetColor();
}
static void ConsolePrint_OnLogError(Exception ex, long? id)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"{DateTime.Now:g}: {ex}");
    Console.ResetColor();
}
