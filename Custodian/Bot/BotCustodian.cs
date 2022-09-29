using Custodian.Commands;
using Custodian.Config;
using Custodian.Logging;
using Custodian.Modules;

using Discord;
using Discord.WebSocket;

namespace Custodian.Bot
{
    public class BotCustodian
    {
        private DiscordSocketClient? client;
        private BotConfig config;
        
        private Dictionary<string, ISlashCommand> commands;
        private List<IModule> modules;

        private ILogger logger;

        private SocketGuild? guild;

        public BotCustodian(BotConfig config, ILogger logger)
        {
            this.config = config;
            this.logger = logger;
        }

        public async Task<bool> SetupAsync()
        {
            var clientConfig = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.All
            };

            client = new DiscordSocketClient(clientConfig);

            if(client == null)
            {
                await logger.LogAsync(LogLevel.ERROR, ">> Discord Socket Client is null, exiting..");
                return false;
            }

            await logger.LogAsync(LogLevel.INFO, "Subscribing to 'Ready' event.");
            client.Ready += _client_Ready;

            await logger.LogAsync(LogLevel.INFO, "Subscribing to 'MessageReceived' event.");
            client.MessageReceived += _client_MessageReceived;

            await logger.LogAsync(LogLevel.INFO, "Subscribing to 'UserVoiceStateUpdated' event.");
            client.UserVoiceStateUpdated += _client_UserVoiceStateUpdated;

            await logger.LogAsync(LogLevel.INFO, "Subscribing to 'SlashCommandExecuted' event.");
            client.SlashCommandExecuted += _client_SlashCommandExecuted;

            await logger.LogAsync(LogLevel.INFO, "Subscribing to 'ReadyMenuExecuted' event.");
            client.SelectMenuExecuted += _client_SelectMenuExecuted;

            await RegisterModules();

            return true;
        }

        private async Task SetStatus()
        {
            await client.SetStatusAsync(UserStatus.Online);
            await client.SetActivityAsync(new Game(" for interactions.", ActivityType.Watching, ActivityProperties.None));
        }

        private async Task RegisterModules()
        {
            modules = new List<IModule>();

            modules.Add(new DirectMessageModule(guild, logger));
            modules.Add(new DynamicVoiceChannelModule(guild, logger));

            if(modules.Count < 1)
            {
                return;
            }

            await logger.LogAsync(LogLevel.INFO, $"Registering '{modules.Count}' module(s).");

            foreach (var module in modules)
            {
                var result = await module.LoadConfig();

                if(result)
                {
                    await logger.LogAsync(LogLevel.INFO, $">> Loaded module '{module.Name}'.");
                }
                else
                {
                    await logger.LogAsync(LogLevel.INFO, $">> Failed to load config, unloading module '{module.Name}'.");
                    modules.Remove(module);
                }
            }
        }

        private async Task RegisterCommands()
        {
            commands = new Dictionary<string, ISlashCommand>();

            var cmdCat = new SlashCommandCat();
            commands.Add(cmdCat.Command, cmdCat);
            
            //var cmdCompile = new SlashCommandCompile();
            //commands.Add(cmdCompile.Command, cmdCompile);
            //var cmdInfo = new SlashCommandInfo();
            //commands.Add(cmdInfo.Command, cmdInfo);
            if(commands.Count < 1)
            {
                return;
            }

            List<SlashCommandBuilder> builders = new List<SlashCommandBuilder>();
            foreach (var command in commands.Values)
            {
                var builder = new SlashCommandBuilder();
                builder.WithName(command.Command);
                builder.WithDescription(command.Description);
                if (command.Options != null && command.Options.Count > 0)
                {
                    foreach (var option in command.Options)
                    {
                        builder.AddOption(option.Name, option.Type, option.Description, option.IsRequired, option.IsDefault, option.IsAutoComplete);
                    }
                }
                builders.Add(builder);
            }

            await logger.LogAsync(LogLevel.INFO, $"Registering '{builders.Count}' command(s).");

            foreach (var builder in builders)
            {
                await guild.CreateApplicationCommandAsync(builder.Build());
                await logger.LogAsync(LogLevel.INFO, $">> Command '{builder.Name}' registered.");
            }

        }

        private async Task _client_SelectMenuExecuted(SocketMessageComponent messageComp)
        {
            foreach(var module in modules)
            {
                await module.OnSelectMenuExecutedAsync(messageComp);
            }
        }

        private async Task _client_SlashCommandExecuted(SocketSlashCommand arg)
        {
            if(commands.ContainsKey(arg.CommandName))
            {
                if(commands.TryGetValue(arg.CommandName, out var command))
                {
                    await logger.LogAsync(LogLevel.INFO, $"[{arg.Channel.Name}] [{arg.User.Username}] ran command: {arg.CommandName}");
                    await command.OnSlashCommandAsync(arg);
                }
            }
        }

        private async Task _client_Ready()
        {
            try
            {
                guild = client.Guilds.FirstOrDefault(g => g.Id == config.GuildId);

                if (guild == null)
                {
                    await logger.LogAsync(LogLevel.ERROR, ">> Guild is null, cannot continue..");
                    return;
                }

                await RegisterCommands();
                await SetStatus();

                await logger.LogAsync(LogLevel.INFO, ">> Bot ready for interaction.");
            }
            catch(Exception ex)
            {
                await logger.LogAsync(LogLevel.ERROR, ">> Bot failed to register commands to guilds with exception: ");
                await logger.LogAsync(LogLevel.ERROR, ex.Message);
            }
        }

        private async Task _client_UserVoiceStateUpdated(SocketUser arg1, SocketVoiceState arg2, SocketVoiceState arg3)
        {
            foreach(var module in modules)
            {
                await module.OnUserVoiceStateUpdated(arg1, arg2, arg3);
            }
        }

        private async Task _client_MessageReceived(SocketMessage socketMessage)
        {
            if (socketMessage.Author.IsBot)
            {
                return;
            }

            if(socketMessage.Channel is SocketDMChannel dmChannel)
            {
                foreach (var module in modules)
                {
                    await module.OnDirectMessageReceivedAsync(socketMessage);
                }
            }
        }

        public async Task StartAsync()
        {
            await logger.LogAsync(LogLevel.INFO, "Logging in..");
            await client.LoginAsync(Discord.TokenType.Bot, config.Token);
            await logger.LogAsync(LogLevel.INFO, ">> Logged in.");
            await logger.LogAsync(LogLevel.INFO, "Starting bot..");
            await client.StartAsync();
            await logger.LogAsync(LogLevel.INFO, ">> Bot started.");

            await Task.Delay(Timeout.Infinite);
        }
    }
}
