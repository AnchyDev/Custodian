using Custodian.Bot;
using Custodian.Models;
using Custodian.Modules;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text.Json;
using System.Reflection;
using Custodian.Shared.Logging;

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
            var config = await Shared.Configuration.Config.GetAsync<BotConfig>(@"./config/config.json", true);

            if (config == null)
            {
                return;
            }

            var services = ConfigureServices(config);
            var serviceProvider = services?.BuildServiceProvider();

            ModuleHandler moduleHandler = serviceProvider?.GetService<ModuleHandler>();
            int loadedModules = await moduleHandler.LoadAsync();

            var logger = serviceProvider.GetService<ILogger>();

            await logger.LogAsync(LogLevel.INFO, $"Found '{loadedModules}' modules, loading..");

            foreach (var module in moduleHandler.Modules)
            {
                var members = module.GetType()
                    .GetMembers(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.IsDefined(typeof(Shared.Modules.ModuleImport)));

                if(members != null && members.Count() > 0)
                {
                    foreach(var member in members)
                    {
                        await logger.LogAsync(LogLevel.INFO, $"Found member '{member.Name}' with attribute ModuleImport.");

                        switch(member.MemberType)
                        {
                            case MemberTypes.Field:
                                var fieldInfo = ((FieldInfo)member);
                                var importObject = serviceProvider.GetService(fieldInfo.FieldType);
                                if(importObject != null)
                                {
                                    await logger.LogAsync(LogLevel.INFO, $"Injecting object with type '{fieldInfo.FieldType}'..");
                                    fieldInfo.SetValue(module, importObject);
                                }
                                else
                                {
                                    await logger.LogAsync(LogLevel.INFO, $"Import Object '{fieldInfo.FieldType}' was null.");
                                }
                                break;
                        }
                    }
                }

                await logger.LogAsync(LogLevel.INFO, $">> Loaded {module.Name}.");
                await module.LoadAsync();
            }

            var bot = serviceProvider?.GetService<BotCustodian>();
            if(await bot.SetupAsync())
            {
                await bot.StartAsync();
            }
        }

        private IServiceCollection? ConfigureServices(BotConfig config)
        {
            var logger = new Shared.Logging.LoggerConsole();
            logger.LogLevel = config.LogLevel;

            var clientConfig = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.All
            };

            var client = new DiscordSocketClient(clientConfig);
            var modules = new List<IModule>();

            var services = new ServiceCollection()
                .AddSingleton<Shared.Logging.ILogger, Shared.Logging.LoggerConsole>(f =>
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