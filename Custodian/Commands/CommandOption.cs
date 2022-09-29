using Discord;

namespace Custodian.Commands
{
    public class CommandOption
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public ApplicationCommandOptionType Type { get; set; }
        public bool IsRequired { get; set; }
        public bool IsDefault { get; set; }
        public bool IsAutoComplete { get; set; }
    }
}
