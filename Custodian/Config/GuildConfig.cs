using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Custodian.Config
{
    public class GuildConfig
    {
        [JsonPropertyName("DYNAMIC_VOICE_CAT_ID")]
        public ulong DynamicVoiceCategoryId { get; set; }

        [JsonPropertyName("DYNAMIC_VOICE_CHAN_ID")]
        public ulong DynamicVoiceChannelId { get; set; }

        public GuildConfig()
        {
            DynamicVoiceCategoryId = 0;
            DynamicVoiceChannelId = 0;
        }
    }
}
