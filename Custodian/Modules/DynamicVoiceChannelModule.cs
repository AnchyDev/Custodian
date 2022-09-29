using Custodian.Logging;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Custodian.Modules
{
    public class DynamicVoiceChannelModule : IModule
    {
        class DynamicVoiceChannelConfig
        {
            [JsonPropertyName("DYNAMIC_VOICE_CHANNEL_ID")]
            public ulong DynamicVoiceChannelId { get; set; }

            [JsonPropertyName("DYNAMIC_VOICE_CATEGORY_ID")]
            public ulong DynamicVoiceCategoryId { get; set; }
        }

        public override string Name { get => "DynamicVoiceChannel"; }
        public override string Description { get => "Allows users to create their own voice channels by joining a specific channel."; }

        private DynamicVoiceChannelConfig config;
        private SocketGuild guild;
        private ILogger logger;
        private List<ulong> trackedChannels;

        public DynamicVoiceChannelModule(SocketGuild guild, ILogger logger)
        {
            this.guild = guild;
            this.logger = logger;
            trackedChannels = new List<ulong>();
        }

        public override async Task<bool> LoadConfig()
        {
            var _config = await GetConfig<DynamicVoiceChannelConfig>();
            if (_config != null)
            {
                config = _config;
                return true;
            }
            else
            {
                await logger.LogAsync(LogLevel.ERROR, "Failed to load config for module '{Name}'.");
                return false;
            }
        }

        public override async Task OnUserVoiceStateUpdated(SocketUser user, SocketVoiceState prevChannel, SocketVoiceState newChannel) 
        {
            var prevVoice = prevChannel.VoiceChannel;
            var newVoice = newChannel.VoiceChannel;

            if (prevVoice != null &&
                trackedChannels.Contains(prevVoice.Id) &&
                prevVoice.ConnectedUsers.Count == 0)
            {
                await logger.LogAsync(LogLevel.INFO, $"No users left in channel '{prevVoice.Name}', deleting..");
                trackedChannels.Remove(prevVoice.Id);
                await prevVoice.DeleteAsync();
                await logger.LogAsync(LogLevel.INFO, ">> Deleted channel.");
            }

            if (newVoice == null)
            {
                return;
            }

            if (newVoice.Id != config.DynamicVoiceChannelId)
            {
                return;
            }

            await logger.LogAsync(LogLevel.INFO, "Detected user in voice channel, moving them to new channel.");

            int currentChannelCount = trackedChannels.Count + 1;
            var freshChannel = await newVoice.Guild.CreateVoiceChannelAsync($"Voice Channel {currentChannelCount}", p =>
            {
                p.CategoryId = config.DynamicVoiceCategoryId;
            });

            trackedChannels.Add(freshChannel.Id);

            await newVoice.Guild.MoveAsync(user as SocketGuildUser, freshChannel);
        }
    }
}
