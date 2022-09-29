namespace Custodian.Logging
{
    public class LoggerConsole : ILogger
    {
        public LogLevel LogLevel { get; set; }

        public async Task LogAsync(LogLevel logLevel, string message)
        {
            if(LogLevel < logLevel)
            {
                return;
            }

            await Task.Run(() => 
            {
                Console.WriteLine(message);
            });
        }
    }
}
