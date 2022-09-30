using Custodian.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Custodian.Models
{
    public class BotConfig
    {
        [JsonPropertyName("BOT_TOKEN")]
        public string Token { get; set; }

        [JsonPropertyName("GUILD_ID")]
        public ulong GuildId { get; set; }

        [JsonPropertyName("LOG_LEVEL")]
        public LogLevel LogLevel { get; set; }

        public BotConfig()
        {
            Token = "BOT_TOKEN_HERE";
            LogLevel = LogLevel.ERROR;
        }
    }
}
