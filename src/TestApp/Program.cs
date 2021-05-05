using System;
using System.Collections.Generic;
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
            var elkSettings = new LogElkSettings()
            {
                IndexPrefix = "test-01",
                User = "spot",
                Password = "63glAuxUz7h6TUbIR79TOVVcp9vX0id2",
                Urls = new Dictionary<string, string>()
                {
                    {"node1", "https://192.168.11.4:9200"},
                    {"node2", "https://192.168.11.5:9200"},
                    {"node3", "https://192.168.11.6:9200"}
                }
            };

            var loggerFactory = LogConfigurator.ConfigureElk(logElkSettings: elkSettings);

            var logger = loggerFactory.CreateLogger("test");

            while (true)
            {
                Log.Logger.Information("Hey serilog");
                logger.LogInformation("Hello world");
                await Task.Delay(1000);
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
