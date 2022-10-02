using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Custodian.Shared.Logging
{
    public class LoggerConsole : ILogger
    {
        public LogLevel LogLevel { get; set; }

        public async Task LogAsync(LogLevel logLevel, string message)
        {
            if (LogLevel < logLevel)
            {
                return;
            }

            await Task.Run(() =>
            {
                Console.WriteLine($"[{logLevel.ToString()}]: {message}");
            });
        }
    }
}
