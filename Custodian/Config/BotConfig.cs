using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Custodian.Config
{
    public class BotConfig
    {
        [JsonPropertyName("BOT_TOKEN")]
        public string Token { get; set; }

        [JsonPropertyName("GUILD_CONFIGS")]
        public Dictionary<ulong, GuildConfig> GuildConfigs { get; set; }

        public BotConfig()
        {
            Token = "BOT_TOKEN_HERE";
            GuildConfigs = new Dictionary<ulong, GuildConfig>();
            GuildConfigs.Add(0, new GuildConfig());
        }
    }
}
