using Custodian.Commands;
using Custodian.Config;

using Discord;
using Discord.WebSocket;

using System;
using System.Text;
using System.Text.Json;

namespace Custodian.Bot
{
    public class BotCustodian
    {
        private readonly DiscordSocketClient _client;
        private BotConfig _config;
        private List<ulong> trackedChannels;
        private Dictionary<string, ISlashCommand> commands;

        public BotCustodian(BotConfig config)
        {
            var clientConfig = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.All
            };
            _client = new DiscordSocketClient(clientConfig);
            _config = config;
            trackedChannels = new List<ulong>();
            commands = new Dictionary<string, ISlashCommand>();

            var cmdCat = new SlashCommandCat();
            commands.Add(cmdCat.Command, cmdCat);

            _client.Ready += _client_Ready;
            _client.MessageReceived += _client_MessageReceived;
            _client.UserVoiceStateUpdated += _client_UserVoiceStateUpdated;
            _client.SlashCommandExecuted += _client_SlashCommandExecuted;
        }

        private async Task _client_SlashCommandExecuted(SocketSlashCommand arg)
        {
            if(commands.ContainsKey(arg.CommandName))
            {
                if(commands.TryGetValue(arg.CommandName, out var command))
                {
                    await command.OnSlashCommandAsync(arg);
                }
            }
        }

        private async Task _client_Ready()
        {
            List<SlashCommandBuilder> builders = new List<SlashCommandBuilder>();
            foreach(var command in commands.Values)
            {
                var builder = new SlashCommandBuilder();
                builder.WithName(command.Command);
                builder.WithDescription(command.Description);
                builders.Add(builder);

            }

            foreach(var guild in _client.Guilds)
            {
                foreach(var builder in builders)
                {
                    await guild.CreateApplicationCommandAsync(builder.Build());
                }
            }

            Console.WriteLine(">> Bot ready for interaction.");
        }

        private async Task _client_UserVoiceStateUpdated(SocketUser arg1, SocketVoiceState arg2, SocketVoiceState arg3)
        {
            var prevChannel = arg2.VoiceChannel;
            var currChannel = arg3.VoiceChannel;

            if(prevChannel != null &&
                trackedChannels.Contains(prevChannel.Id) &&
                prevChannel.ConnectedUsers.Count == 0)
            {
                Console.WriteLine($"No users left in channel '{prevChannel.Name}', deleting..");
                trackedChannels.Remove(prevChannel.Id);
                await prevChannel.DeleteAsync();
                Console.WriteLine(">> Deleted channel.");
            }

            if(currChannel == null)
            {
                return;
            }

            if (currChannel.Id != _config.DynamicVoiceChannelId)
            {
                return;
            }

            Console.WriteLine("Detected user in voice channel, moving them to new channel.");

            int currentChannelCount = trackedChannels.Count + 1;
            var newChannel = await currChannel.Guild.CreateVoiceChannelAsync($"Voice Channel {currentChannelCount}", p =>
            {
                p.CategoryId = _config.DynamicVoiceCategoryId;
            });

            trackedChannels.Add(newChannel.Id);
            
            await currChannel.Guild.MoveAsync(arg1 as SocketGuildUser, newChannel);
        }

        private async Task _client_MessageReceived(SocketMessage arg)
        {
            if(!arg.Author.IsBot)
            {
                Console.WriteLine($"[{arg.Channel.Name}] [{arg.Author.Username}]: {arg.CleanContent}");
            }
        }

        public async Task StartAsync()
        {
            Console.WriteLine("Logging in..");
            await _client.LoginAsync(Discord.TokenType.Bot, _config.Token);
            Console.WriteLine(">> Logged in.");
            Console.WriteLine("Starting bot..");
            await _client.StartAsync();
            Console.WriteLine(">> Bot started.");

            await Task.Delay(Timeout.Infinite);
        }
    }
}
