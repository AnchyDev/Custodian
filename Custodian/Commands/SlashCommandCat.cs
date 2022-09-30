using Discord;
using Discord.WebSocket;

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Custodian.Commands
{
    public class SlashCommandCat : ISlashCommand
    {
        public string Command { get => "cat"; }
        public string Description { get => "Retrieves a picture of a cat."; }

        public List<CommandOption> Options { get => null; }

        private HttpClient _httpClient;

        public SlashCommandCat(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task OnSlashCommandAsync(SocketSlashCommand command)
        {
            Console.WriteLine("Fetching cat from api..");
            await command.DeferAsync();
            var response = await _httpClient.GetStringAsync("https://api.thecatapi.com/v1/images/search");
            using var reader = new MemoryStream(Encoding.UTF8.GetBytes(response));
            var catApi = await JsonSerializer.DeserializeAsync<List<CatApi>>(reader);

            if (catApi != null && catApi[0] != null)
            {
                await command.ModifyOriginalResponseAsync(p =>
                {
                    p.Embed = new EmbedBuilder().WithImageUrl(catApi[0].Url).Build();
                });
                Console.WriteLine(">> Cat found.");
            }
            else
            {
                await command.ModifyOriginalResponseAsync(p =>
                {
                    p.Content = "Sorry! Failed to load a :cat:";
                });
                Console.WriteLine(">> Failed to load cat.");
            }
        }

        public class CatApi
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("url")]
            public string Url { get; set; }

            [JsonPropertyName("width")]
            public int Width { get; set; }

            [JsonPropertyName("height")]
            public int Height { get; set; }
        }
    }
}