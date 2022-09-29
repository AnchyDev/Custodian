using Discord.WebSocket;
using System.Text.Json;

namespace Custodian.Modules
{
    public abstract class IModule
    {
        public abstract string Name { get; }
        public abstract string Description { get; }

        public async Task<T?> GetConfig<T>() where T : new()
        {
            string path = "./config/modules/";
            string config = @$"{Name}.config.json";
            string fullPath = Path.Combine(path, config);

            if(!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            if(!File.Exists(fullPath))
            {
                using (var fs = new FileStream(fullPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    await JsonSerializer.SerializeAsync<T>(fs, new T());
                }
            }

            using (var fs = new FileStream(fullPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                return await JsonSerializer.DeserializeAsync<T>(fs);
            }
        }

        public virtual Task OnDirectMessageReceivedAsync(SocketMessage message) => Task.CompletedTask;
        public virtual Task OnSelectMenuExecutedAsync(SocketMessageComponent messageComp) => Task.CompletedTask;
        public virtual Task OnUserVoiceStateUpdated(SocketUser user, SocketVoiceState prevChannel, SocketVoiceState newChannel) => Task.CompletedTask;
        public virtual Task<bool> LoadConfig() => (Task<bool>)Task.CompletedTask;
    }
}
