using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Custodian.Bot
{
    public class BotCustodian
    {
        private readonly DiscordSocketClient _client;

        public BotCustodian()
        {
            _client = new DiscordSocketClient();
        }

        public async Task StartAsync()
        {
            await _client.LoginAsync(Discord.TokenType.Bot, Environment.GetEnvironmentVariable("CUSTODIAN_TOKEN"));
            await _client.StartAsync();
            await Task.Delay(Timeout.Infinite);
        }
    }
}
