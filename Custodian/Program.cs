using Custodian.Bot;
using Custodian.Models;
using Custodian.Modules;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Reflection;
using Custodian.Shared.Logging;
using Custodian.Shared.Configuration;

namespace Custodian
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var config = await Config.GetAsync<BotConfig>(@"./config/config.json", true);

            if(config == null)
            {
                throw new NullReferenceException("Config is null!");
            }

            var serviceCollection = ConfigureServices(config);
            var serviceProvider = serviceCollection?.BuildServiceProvider();

            var moduleHandler = serviceProvider?.GetService<ModuleHandler>();
            var logger = serviceProvider?.GetService<ILogger>();

            if(moduleHandler == null)
            {
                throw new NullReferenceException("ModuleHandler is null!");
            }

            int modulesLoaded = await moduleHandler.LoadAsync();

            if(modulesLoaded > 0)
            {
                await logger.LogAsync(LogLevel.INFO, $"Loaded '{modulesLoaded}' module(s) from disk.");
                await moduleHandler.InjectAsync(serviceProvider);

                await logger.LogAsync(LogLevel.INFO, $"Calling module(s) LoadAsync..");
                foreach(var module in moduleHandler.Modules)
                {
                    await module.LoadAsync();
                }
            }
            else
            {
                await logger.LogAsync(LogLevel.INFO, $"No modules found.");
            }

            var bot = serviceProvider?.GetService<BotCustodian>();
            if (await bot.SetupAsync())
            {
                await bot.StartAsync();
            }
        }

        private static IServiceCollection? ConfigureServices(BotConfig config)
        {
            var logger = new LoggerConsole();
            logger.LogLevel = config.LogLevel;

            var clientConfig = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.All
            };

            var client = new DiscordSocketClient(clientConfig);
            var modules = new List<IModule>();

            var services = new ServiceCollection()
                .AddSingleton<ILogger, LoggerConsole>(f =>
                {
                    return logger;
                })
                .AddSingleton<BotConfig>(config)
                .AddSingleton<BotCustodian>()
                .AddSingleton<DiscordSocketClient>(client)
                .AddSingleton<HttpClient>()
                .AddSingleton<ModuleHandler>();

            return services;
        }
    }
}