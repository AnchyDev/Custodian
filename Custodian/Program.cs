﻿using Custodian.Bot;
using Custodian.Models;
using Custodian.Logging;
using Custodian.Modules;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
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
            //var config = await LoadConfig();
            var config = await Custodian.Shared.Configuration.Config.GetAsync<BotConfig>(@"./config/config.json", true);
            if (config == null)
            {
                return;
            }

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