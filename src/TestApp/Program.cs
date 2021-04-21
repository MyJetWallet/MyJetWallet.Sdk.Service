using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using Serilog;
using Serilog.Events;
using ILogger = Serilog.ILogger;

namespace TestApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var loggerFactory = LogConfigurator.ConfigureElk();

            var logger = loggerFactory.CreateLogger("test");

            

            while (true)
            {
                Log.Logger.Information("Hey serilog");
                logger.LogInformation("Hello world");
                await Task.Delay(200);
            }
        }
    }

    public class MyLogger: Serilog.ILogger
    {
        private readonly ILogger _logger;

        public MyLogger(Serilog.ILogger logger)
        {
            _logger = logger;
        }

        public void Write(LogEvent logEvent)
        {
            _logger.Write(logEvent);
            Console.WriteLine("Hey from logger");
        }
    }
}
