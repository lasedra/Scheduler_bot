using Microsoft.Extensions.Configuration;
using Scheduler_bot.Models;
using PRTelegramBot.Core;

IConfiguration AppConfig = new ConfigurationBuilder().AddJsonFile("appconfig.json", optional: false, reloadOnChange: true).Build();
SchedulerDbContext DbContext = new(AppConfig.GetConnectionString("localhost"));
PRBot botClient = new(options =>
{
    options.Token = AppConfig.GetSection("Bot token").Value;
    options.ClearUpdatesOnStart = true;
});

botClient.OnLogCommon += ConsolePrint_OnLogCommon;
botClient.OnLogError += ConsolePrint_OnLogError;

await botClient.Start();

while (true)
    if (Console.ReadLine().ToLower() == "exit")
        Environment.Exit(0);

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
