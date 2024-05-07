using PRTelegramBot.Interface;

namespace Scheduler_bot
{
    public class UserCache : ITelegramCache
    {
        public string Login { get; set; } = null!;

        public string Password { get; set; } = null!;

        public bool ClearData()
        {
            Login = string.Empty;
            Login = string.Empty;
            return true;
        }
    }
}
