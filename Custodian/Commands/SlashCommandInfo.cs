using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Custodian.Commands
{
    public class SlashCommandInfo : ISlashCommand
    {
        public string Command { get => "info"; }

        public string Description { get => "Queries information about the current user."; }

        public List<CommandOption> Options
        {
            get => new List<CommandOption>()
            {
                new CommandOption()
                {
                    Name = "user",
                    Description = "The user to query.",
                    Type = Discord.ApplicationCommandOptionType.User,
                    IsRequired = true
                }
            };
        }

        public async Task OnSlashCommandAsync(SocketSlashCommand command)
        {
            await command.DeferAsync();
            var cmdOption = command.Data.Options.First(c => c.Name == "user");

            if(cmdOption == null || cmdOption.Value == null || !(cmdOption.Value is SocketUser))
            {
                await command.ModifyOriginalResponseAsync(p =>
                {
                    p.Content = $"Failed to fetch information for that user.";
                });

                return;
            }

            var user = cmdOption.Value as SocketUser;
            var sb = new StringBuilder();

            sb.AppendLine("```");
            sb.AppendLine($"{user.CreatedAt.ToString()}");
            sb.AppendLine("```");

            await command.ModifyOriginalResponseAsync(p =>
            {
                p.Content = sb.ToString();
            });

        }
    }
}
