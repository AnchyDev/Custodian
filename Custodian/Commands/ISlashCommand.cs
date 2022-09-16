using Custodian.Config;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Custodian.Commands
{
    public interface ISlashCommand
    {
        string Command { get; }
        string Description { get; }
        Task OnSlashCommandAsync(SocketSlashCommand command);
    }
}
