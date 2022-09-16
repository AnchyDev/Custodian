using Custodian.Config;
using Discord;
using Discord.WebSocket;

using System;

namespace Custodian.Bot
{
    public class BotCustodian
    {
        private readonly DiscordSocketClient _client;
        private BotConfig _config;
        private List<ulong> trackedChannels;

        public BotCustodian(BotConfig config)
        {
            var clientConfig = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.All
            };
            _client = new DiscordSocketClient(clientConfig);
            _config = config;
            trackedChannels = new List<ulong>();

            _client.Ready += _client_Ready;
            _client.MessageReceived += _client_MessageReceived;
            _client.UserVoiceStateUpdated += _client_UserVoiceStateUpdated;
            _client.SlashCommandExecuted += _client_SlashCommandExecuted;
        }

        private async Task _client_SlashCommandExecuted(SocketSlashCommand arg)
        {
            if(arg.CommandName.Equals("test"))
            {
                await arg.RespondAsync("Hello!");
            }
        }

        private async Task _client_Ready()
        {
            Console.WriteLine(">> Bot ready for interaction.");

            var cmd = new SlashCommandBuilder();
            cmd.WithName("test");
            cmd.WithDescription("This is a test command.");

            foreach(var guild in _client.Guilds)
            {
                await guild.CreateApplicationCommandAsync(cmd.Build());
            }
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
            Console.WriteLine($"[{arg.Channel.Name}] [{arg.Author.Username}]: {arg.CleanContent}");
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
