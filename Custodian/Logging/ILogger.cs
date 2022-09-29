using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Custodian.Logging
{
    public interface ILogger
    {
        public Task LogAsync(LogLevel logLevel, string message);
        public LogLevel LogLevel { get; set; }
    }
}
