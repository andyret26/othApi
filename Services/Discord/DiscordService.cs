
using Discord.WebSocket;

namespace othApi.Services.Discord
{
    public class DiscordService(DiscordSocketClient discord) : IDiscordService
    {
        private readonly DiscordSocketClient _discord = discord;
        private readonly ulong _guild = 622429522154749992;
        private readonly ulong _channel = 1173892318244376596;

        public async Task SendMessage(string message)
        {
            try
            {
                var guild = _discord.GetGuild(_guild);
                var channel = guild.GetTextChannel(_channel);

                await channel.SendMessageAsync(message);

            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.ToString());
                Console.WriteLine("Error Notifying");
                return;
            }
            return;
        }
    }
}
