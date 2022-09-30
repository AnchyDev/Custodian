using System;
using System.Collections.Generic;
using System.Text;

namespace Custodian.Shared.Module
{
    public abstract class Module
    {
        public abstract string Name { get; }
        public abstract string Description { get; }
    }
}
