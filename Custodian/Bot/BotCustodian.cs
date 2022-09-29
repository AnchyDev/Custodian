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
        private DiscordSocketClient _client;
        private BotConfig config;
        
        private Dictionary<string, ISlashCommand> commands;
        private List<IModule> modules;

        private ILogger logger;

        public BotCustodian(BotConfig config, ILogger logger)
        {
            this.config = config;
            this.logger = logger;

            SetupClient();
            RegisterCommands();
            SubscribeToEvents();
            SetStatus();
        }

        private void SetupClient()
        {
            var clientConfig = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.All
            };

            _client = new DiscordSocketClient(clientConfig);
        }

        private void SetStatus()
        {
            _client.SetStatusAsync(UserStatus.Online);
            _client.SetActivityAsync(new Game(" for interactions.", ActivityType.Watching, ActivityProperties.None));
        }

        private async Task RegisterModules()
        {
            modules = new List<IModule>();

            var guild = _client.Guilds.FirstOrDefault(g => g.Id == config.GuildId);
            if(guild == null)
            {
                await logger.LogAsync(LogLevel.ERROR, "[RegisterModules] Guild is null!!");
                return;
            }

            modules.Add(new DirectMessageModule(guild));
            modules.Add(new DynamicVoiceChannelModule(guild));

            foreach(var module in modules)
            {
                await module.LoadConfig();
                await logger.LogAsync(LogLevel.INFO, $"Loaded module '{module.Name}'.");
            }
        }

        private void RegisterCommands()
        {
            commands = new Dictionary<string, ISlashCommand>();
            //var cmdCat = new SlashCommandCat();
            //commands.Add(cmdCat.Command, cmdCat);
            //
            //var cmdCompile = new SlashCommandCompile();
            //commands.Add(cmdCompile.Command, cmdCompile);
            //var cmdInfo = new SlashCommandInfo();
            //commands.Add(cmdInfo.Command, cmdInfo);
        }

        private void SubscribeToEvents()
        {
            _client.Ready += _client_Ready;
            _client.MessageReceived += _client_MessageReceived;
            _client.UserVoiceStateUpdated += _client_UserVoiceStateUpdated;
            _client.SlashCommandExecuted += _client_SlashCommandExecuted;
            _client.SelectMenuExecuted += _client_SelectMenuExecuted;
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

                var guild = _client.Guilds.First(g => g.Id == config.GuildId);

                foreach (var builder in builders)
                {
                    await guild.CreateApplicationCommandAsync(builder.Build());
                }

                await RegisterModules();

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
            await _client.LoginAsync(Discord.TokenType.Bot, config.Token);
            await logger.LogAsync(LogLevel.INFO, ">> Logged in.");
            await logger.LogAsync(LogLevel.INFO, "Starting bot..");
            await _client.StartAsync();
            await logger.LogAsync(LogLevel.INFO, ">> Bot started.");

            await Task.Delay(Timeout.Infinite);
        }
    }
}
