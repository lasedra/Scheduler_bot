using PRTelegramBot.Models;
using PRTelegramBot.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Scheduler_bot
{
    public static class MainMenu_RplyKybrd
    {
        public static async Task ShowMainMenu(ITelegramBotClient botClient, Update update)
        {
            var menuContent = new List<KeyboardButton>()
            { 
                new("⤵Назначить занятие"), 
                new("ещё четотам") 
            };
            var menu = MenuGenerator.ReplyKeyboard(1, menuContent, mainMenu: "главное меню?");

            await PRTelegramBot.Helpers.Message.Send(botClient, update,
                msg: "Чем могу быть полезен?",
                option: new OptionMessage() { MenuReplyKeyboardMarkup = menu });
        }
    }
}
