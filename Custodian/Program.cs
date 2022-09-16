using Custodian.Bot;
using Custodian.Config;

using System;
using System.Text.Json;

namespace Custodian
{
    public class Program
    {
        private static BotConfig _config;

        public static async Task Main(string[] args)
        {
            var config = @"./config.json";

            Console.WriteLine("Loading config.json..");
            if(File.Exists(config))
            {
                using (var fs = new FileStream(config, FileMode.Open, FileAccess.Read))
                {
                    _config = await JsonSerializer.DeserializeAsync<BotConfig>(fs);
                }
            }
            else
            {
                Console.WriteLine(">> No config.json found, creating new..");
                Console.WriteLine(">> Make sure to configure the config.json and restart Custodian.");
                _config = new BotConfig();
                using (var fs = new FileStream(config, FileMode.Create, FileAccess.Write))
                {
                    await JsonSerializer.SerializeAsync<BotConfig>(fs, _config);
                }
                Console.WriteLine(">> Exiting..");
                return;
            }

            if(_config == null)
            {
                Console.WriteLine(">> Failed to load config.json..");
                Console.WriteLine(">> Exiting..");
                return;
            }

            Console.WriteLine(">> Config.json finished loading.");

            var bot = new BotCustodian(_config);
            await bot.StartAsync();
        }
    }
}