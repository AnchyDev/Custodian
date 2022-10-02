using Custodian.Shared.Logging;
using Custodian.Shared.Modules;
using Discord.WebSocket;
using System.Text.Json.Serialization;

namespace Custodian.DirectMessage.Modules
{
    public class ModuleDynamicVoiceChannel : Module
    {
        public override string Name { get => "DynamicVoiceChannel"; }
        public override string Description { get => "Allows users to create their own voice channels by joining a specific channel."; }

        [ModuleImport]
        private ILogger logger;

        private DynamicVoiceChannelConfig config;

        private List<ulong> trackedChannels;

        public override async Task LoadAsync()
        {
            await Task.Run(() =>
            {
                trackedChannels = new List<ulong>();
            });

            config = await this.GetConfig<DynamicVoiceChannelConfig>();
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

        class DynamicVoiceChannelConfig
        {
            [JsonPropertyName("DYNAMIC_VOICE_CHANNEL_ID")]
            public ulong DynamicVoiceChannelId { get; set; }

            [JsonPropertyName("DYNAMIC_VOICE_CATEGORY_ID")]
            public ulong DynamicVoiceCategoryId { get; set; }
        }
    }
}
