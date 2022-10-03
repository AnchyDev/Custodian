using Custodian.Shared.Configuration;
using Discord.WebSocket;
using System.Text.Json;

namespace Custodian.Shared.Modules
{
    public abstract class Module
    {
        public abstract string Name { get; }
        public abstract string Description { get; }

        public virtual Task OnClientReadyAsync(SocketGuild guild) => Task.CompletedTask;
        public virtual Task OnDirectMessageReceivedAsync(SocketMessage message) => Task.CompletedTask;
        public virtual Task OnSelectMenuExecutedAsync(SocketMessageComponent messageComp) => Task.CompletedTask;
        public virtual Task OnUserVoiceStateUpdated(SocketUser user, SocketVoiceState prevChannel, SocketVoiceState newChannel) => Task.CompletedTask;
        public virtual Task<bool> LoadConfig() => Task.FromResult<bool>(true);
        public virtual async Task LoadAsync() => await Task.CompletedTask;

        public async Task<T?> GetConfig<T>() where T : new()
        {
            string path = "./config/modules/";
            string config = @$"{Name}.config.json";
            string fullPath = Path.Combine(path, config);

            return await Config.GetAsync<T>(fullPath, true);
        }
    }
}
