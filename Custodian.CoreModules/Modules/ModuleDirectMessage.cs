using Custodian.Shared.Logging;
using Custodian.Shared.Modules;
using Discord;
using Discord.WebSocket;
using System.Text;
using System.Text.Json.Serialization;

namespace Custodian.CoreModules.Modules
{
    public class ModuleDirectMessage : Module
    {
        public override string Name { get => "DirectMessage"; }
        public override string Description { get => "Allows users to complete actions in a direct message channel with the bot."; }

        [ModuleImport]
        private ILogger logger;

        private SocketGuild guild;

        private DirectMessageConfig config;

        private List<ulong>? usersReporting;

        public override async Task LoadAsync()
        {
            await Task.Run(() =>
            {
                usersReporting = new List<ulong>();
            });

            config = await this.GetConfig<DirectMessageConfig>();
        }

        public override async Task OnClientReadyAsync(SocketGuild guild)
        {
            this.guild = guild;
        }

        public override async Task OnDirectMessageReceivedAsync(SocketMessage message)
        {
            if (usersReporting.Contains(message.Author.Id))
            {
                usersReporting.Remove(message.Author.Id);
                await message.Channel.SendMessageAsync(text: "Your message has been logged.");

                if (guild == null)
                {
                    await logger.LogAsync(LogLevel.ERROR, "[DirectMessageModule] Guild was null!");
                    return;
                }

                var forum = guild.GetForumChannel(config.ReportForumChannelId);
                var threads = await forum.GetActiveThreadsAsync();
                var thread = threads.FirstOrDefault(t => t.Name.Split(' ')[1] == $"{message.Author.Id}");

                if (thread != null)
                {
                    await thread.SendMessageAsync(text: $"{message.Author.Username}: {message.CleanContent}");
                }
                else
                {
                    await forum.CreatePostAsync(title: $"[{message.Author.Username}] {message.Author.Id}",
                        archiveDuration: ThreadArchiveDuration.OneWeek,
                        text: $"{message.Author.Username}: {message.CleanContent}");
                }

                await PromptMessage("If there is anything else I can assist you with,",
                    "select an option from the widgets below to continue.",
                    message.Channel);
                return;
            }

            await PromptMessage("Hi! I am the Custodian for the TechSpace Discord.",
                    "Please select an option from the widgets below to continue.",
                    message.Channel);
        }

        public override async Task OnSelectMenuExecutedAsync(SocketMessageComponent messageComp)
        {
            await messageComp.DeferAsync();

            if (messageComp.Data.CustomId == "menu-1" && messageComp.Data.Values.First() == "opt-a")
            {
                await messageComp.FollowupAsync("You have selected 'Report', please leave a message and it will be forwarded.");

                if (!usersReporting.Contains(messageComp.User.Id))
                {
                    usersReporting.Add(messageComp.User.Id);
                }
            }
        }


        private async Task PromptMessage(string prompt, string subPrompt, IMessageChannel channel)
        {
            var compBuilder = new ComponentBuilder();
            var menuBuilder = new SelectMenuBuilder()
            .WithPlaceholder("Select an option")
            .WithCustomId("menu-1")
            .WithMinValues(1)
            .WithMaxValues(1)
            .AddOption("Report", "opt-a", "Report an issue that will be forwarded to the TechSpace staff.");
            compBuilder.WithSelectMenu(menuBuilder);

            var sb = new StringBuilder();
            sb.AppendLine(prompt);
            sb.AppendLine("Please choose an option from the widgets below to continue.");

            await channel.SendMessageAsync(text: sb.ToString(), components: compBuilder.Build());
        }

        class DirectMessageConfig
        {
            [JsonPropertyName("REPORT_FORUM_CHANNEL_ID")]
            public ulong ReportForumChannelId { get; set; }
        }

    }
}
