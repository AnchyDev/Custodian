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

        [JsonPropertyName("DYNAMIC_VOICE_CAT_ID")]
        public ulong DynamicVoiceCategoryId { get; set; }

        [JsonPropertyName("DYNAMIC_VOICE_CHAN_ID")]
        public ulong DynamicVoiceChannelId { get; set; }

        public BotConfig()
        {
            Token = "BOT_TOKEN_HERE";
            DynamicVoiceCategoryId = 0;
            DynamicVoiceChannelId = 0;
        }
    }
}
