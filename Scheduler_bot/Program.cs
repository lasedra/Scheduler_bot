using PRTelegramBot.Core;
using Scheduler_bot;

PRBot BotClient = new(options =>
{
    options.Token = Dispatcher.AppConfig.GetSection("Bot token").Value ?? "NO_BOT_TOKEN";
    options.ClearUpdatesOnStart = true;
});

BotClient.OnLogCommon += ConsolePrint_OnLogCommon;
BotClient.OnLogError += ConsolePrint_OnLogError;
await BotClient.Start();

while (true)
    if (Console.ReadLine()!.ToLower() == "exit")
    {
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