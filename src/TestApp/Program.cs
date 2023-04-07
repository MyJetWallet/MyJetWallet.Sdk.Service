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
            // var httpClient = new HttpClient();
            // httpClient.Timeout = TimeSpan.FromSeconds(5);
            //
            // try
            // {
            //    var resp = httpClient.GetAsync("https://192.168.11.4:9200").GetAwaiter().GetResult();
            //     
            //     Console.WriteLine(resp.StatusCode);
            // }
            // catch (Exception e)
            // {
            //     Console.WriteLine(e);
            // }
            //
            // return;
            
            
            
            var elkSettings = new LogElkSettings
            {
                IndexPrefix = "test-01",
                User = "***",
                Password = "***",
                Urls = new Dictionary<string, string>
                {
                    {"node1", "https://***:9243"},
                    {"node2", "https://***:9243"},
                    {"node3", "https://***:9243"}
                }
            };

            var loggerFactory = LogConfigurator.ConfigureElk_v2(logElkSettings: elkSettings);

            var logger = loggerFactory.CreateLogger("test");

            while (true)
            {
                Log.Logger.Information("Hey serilog");
                logger.LogInformation("Hello world");
                await Task.Delay(1000);
            }
        }
    }

    public class MyLogger: ILogger
    {
        private readonly ILogger _logger;

        public MyLogger(ILogger logger)
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
