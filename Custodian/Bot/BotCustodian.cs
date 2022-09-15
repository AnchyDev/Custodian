using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Custodian.Bot
{
    public class BotCustodian
    {
        private readonly DiscordSocketClient _client;
        private List<ulong> trackedChannels;

        public BotCustodian()
        {
            var clientConfig = new DiscordSocketConfig()
            {
                GatewayIntents = Discord.GatewayIntents.All
            };
            _client = new DiscordSocketClient(clientConfig);
            trackedChannels = new List<ulong>();

            _client.MessageReceived += _client_MessageReceived;
            _client.UserVoiceStateUpdated += _client_UserVoiceStateUpdated;
        }

        private async Task _client_UserVoiceStateUpdated(SocketUser arg1, SocketVoiceState arg2, SocketVoiceState arg3)
        {
            var voiceChannel = arg3.VoiceChannel;

            if(trackedChannels.Contains(arg2.VoiceChannel.Id) &&
                arg2.VoiceChannel.ConnectedUsers.Count == 0)
            {
                Console.WriteLine($"No users left in channel '{arg2.VoiceChannel.Name}', deleting..");
                trackedChannels.Remove(arg2.VoiceChannel.Id);
                await arg2.VoiceChannel?.DeleteAsync();
                Console.WriteLine(">> Deleted channel.");
            }

            if (voiceChannel?.Id != 814828603724398592)
            {
                return;
            }

            Console.WriteLine("Detected user in voice channel, moving them to new channel.");

            int currentChannelCount = trackedChannels.Count + 1;
            var newChannel = await voiceChannel?.Guild?.CreateVoiceChannelAsync($"Voice Channel {currentChannelCount}", p =>
            {
                p.CategoryId = 740999436876120126;
            });

            trackedChannels.Add(newChannel.Id);
            
            await voiceChannel?.Guild?.MoveAsync(arg1 as SocketGuildUser, newChannel);
        }

        private async Task _client_MessageReceived(SocketMessage arg)
        {
            Console.WriteLine($"[{arg.Channel.Name}] [{arg.Author.Username}]: {arg.CleanContent}");
        }

        public async Task StartAsync()
        {
            var botToken = "MTAxOTg1MTgzMjU2ODMxNjAxNA.Gk-vHi.7_V1Zr577tc93yytGuDssB1t0je-jaMOXl5emA";

            Console.WriteLine("Logging in..");
            await _client.LoginAsync(Discord.TokenType.Bot, botToken);
            Console.WriteLine(">> Logged in.");
            Console.WriteLine("Starting bot..");
            await _client.StartAsync();
            Console.WriteLine(">> Bot started.");

            await Task.Delay(Timeout.Infinite);
        }
    }
}
