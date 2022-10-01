using Custodian.Bot;
using Custodian.Models;
using Custodian.Logging;
using Custodian.Modules;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text.Json;
using System.Reflection;

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
            //var config = await LoadConfig();
            var config = await Custodian.Shared.Configuration.Config.GetAsync<BotConfig>(@"./config/config.json", true);
            if (config == null)
            {
                return;
            }

            await LoadModules();

            var services = ConfigureServices(config);
            var serviceProvider = services?.BuildServiceProvider();

            var modules = serviceProvider?.GetService<List<IModule>>();
            var _modules = serviceProvider?.GetServices<IModule>();
            modules?.AddRange(_modules);

            var bot = serviceProvider?.GetService<BotCustodian>();
            if(await bot.SetupAsync())
            {
                await bot.StartAsync();
            }
        }

        private async Task LoadModules()
        {
            var modulesPath = "./modules";

            if(!Directory.Exists(modulesPath))
            {
                Console.WriteLine($"'{modulesPath}' directory does not exist, creating..");
                Directory.CreateDirectory(modulesPath);
            }

            var modules = Directory.GetFiles(modulesPath, "*.dll");

            if(modules.Length < 1)
            {
                Console.WriteLine("No modules found.");
                return;
            }

            List<Assembly> validModules = new List<Assembly>();

            foreach(var module in modules)
            {
                var modulePath = Path.GetFullPath(module);
                try
                {
                    var assembly = Assembly.LoadFile(modulePath);
                    var types = assembly.GetTypes();

                    if (types.Any(t => typeof(Custodian.Shared.Modules.Module).IsAssignableFrom(t)))
                    {
                        validModules.Add(assembly);
                        continue;
                    }
                }
                catch(Exception)
                {
                    continue;
                }
            }

            if(validModules.Count < 1)
            {
                Console.WriteLine("No modules found.");
                return;
            }

            Console.WriteLine($"Loading '{validModules.Count}' module(s)..");

            foreach(var module in validModules)
            {
                var types = module.GetTypes();

                foreach(var type in types)
                {
                    if(typeof(Custodian.Shared.Modules.Module).IsAssignableFrom(type))
                    {
                        var m = Activator.CreateInstance(type) as Shared.Modules.Module;
                        Console.WriteLine($"Found '{m.Name}'");
                    }
                }
            }
        }

        private IServiceCollection? ConfigureServices(BotConfig config)
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
                .AddSingleton<List<IModule>>(modules)
                .AddSingleton<IModule, DirectMessageModule>()
                .AddSingleton<IModule, DynamicVoiceChannelModule>()
                .AddSingleton<DiscordSocketClient>(client)
                .AddSingleton<HttpClient>();

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