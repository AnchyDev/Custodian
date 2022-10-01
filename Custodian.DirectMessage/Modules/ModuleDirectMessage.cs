using Custodian.Shared.Modules;

namespace Custodian.DirectMessage.Modules
{
    public class ModuleDirectMessage : Module
    {
        public override string Name { get => "DirectMessage"; }
        public override string Description { get => "Allows users to complete actions in a direct message channel with the bot."; }
    }
}
