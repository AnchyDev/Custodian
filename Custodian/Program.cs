using Custodian.Bot;
using System;

namespace Custodian
{
    public class Program
    {
        public async Task Main(string[] args)
        {
            var bot = new BotCustodian();
            await bot.StartAsync();
        }
    }
}