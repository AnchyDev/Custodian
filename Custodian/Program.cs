using Custodian.Bot;
using Custodian.Config;
using Custodian.Logging;
using System;
using System.Text.Json;

namespace Custodian
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await new Program().StartAsync();
        }

        public async Task StartAsync()
        {
            var config = await LoadConfig();
            if(config == null)
            {
                return;
            }
            var logger = new LoggerConsole();
            logger.LogLevel = config.LogLevel;
            var bot = new BotCustodian(config, logger);
            await bot.StartAsync();
        }

        private async Task<BotConfig?> LoadConfig()
        {
            var path = @"./config";
            var config = @"./config.json";
            var fullPath = Path.Combine(path, config);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            BotConfig? _config;

            Console.WriteLine("Loading config.json..");
            if (File.Exists(fullPath))
            {
                using (var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
                {
                    _config = await JsonSerializer.DeserializeAsync<BotConfig>(fs);
                }
            }
            else
            {
                Console.WriteLine(">> No config.json found, creating new..");
                Console.WriteLine(">> Make sure to configure the config.json and restart Custodian.");
                _config = new BotConfig();
                using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
                {
                    await JsonSerializer.SerializeAsync<BotConfig>(fs, _config);
                }
                Console.WriteLine(">> Exiting..");
                return null;
            }

            if (_config == null)
            {
                Console.WriteLine(">> Failed to load config.json..");
                Console.WriteLine(">> Exiting..");
                return null;
            }

            Console.WriteLine(">> Config.json finished loading.");

            return _config;
        }
    }
}