using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace MyJetWallet.Sdk.Service
{
    public static class LogConfigurator
    {
        public static ILoggerFactory Configure(
            string productName = default,
            string seqServiceUrl = default)
        {
            Console.WriteLine($"App - name: {ApplicationEnvironment.AppName}");
            Console.WriteLine($"App - version: {ApplicationEnvironment.AppVersion}");

            IConfigurationRoot configRoot = BuildConfigRoot();

            var config = new LoggerConfiguration()
                .ReadFrom.Configuration(configRoot)
                .Enrich.FromLogContext()
                .Enrich.WithExceptionData()
                .Enrich.WithCorrelationIdHeader();

            SetupProperty(productName, config);

            SetupConsole(configRoot, config);

            SetupSeq(configRoot, config, seqServiceUrl);

            Log.Logger = config.CreateLogger();

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Log.Fatal((Exception)e.ExceptionObject, "Application has been terminated unexpectedly");
                Log.CloseAndFlush();
            };
            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                Log.CloseAndFlush();
            };

            return new LoggerFactory().AddSerilog();
        }

        private static IConfigurationRoot BuildConfigRoot()
        {
            var configBuilder = new ConfigurationBuilder();

            configBuilder
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{ApplicationEnvironment.Environment}.json", optional: true)
                .AddEnvironmentVariables();

            var configRoot = configBuilder.Build();
            return configRoot;
        }

        private static void SetupProperty(string productName, LoggerConfiguration config)
        {
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            config
                .Enrich.FromLogContext()
                .Enrich.WithProperty("app-name", ApplicationEnvironment.AppName)
                .Enrich.WithProperty("app-version", ApplicationEnvironment.AppVersion)
                .Enrich.WithProperty("host-name", ApplicationEnvironment.HostName ?? ApplicationEnvironment.UserName)
                .Enrich.WithProperty("environment", ApplicationEnvironment.Environment)
                .Enrich.WithProperty("started-at", ApplicationEnvironment.StartedAt);

            if (productName != default)
            {
                config.Enrich.WithProperty("product-name", productName);
            }
        }

        private static void SetupConsole(IConfigurationRoot configRoot, LoggerConfiguration config)
        {
            var logLevel = configRoot["ConsoleOutputLogLevel"];

            if (!string.IsNullOrEmpty(logLevel) && Enum.TryParse<LogEventLevel>(logLevel, out var restrictedToMinimumLevel))
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Env - ConsoleOutputLogLevel: {restrictedToMinimumLevel}");
                Console.ForegroundColor = color;

                config.WriteTo.Console(restrictedToMinimumLevel);
            }
            else if (logLevel == "Default")
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Env - ConsoleOutputLogLevel: <default>");
                Console.ForegroundColor = color;

                config.WriteTo.Console();
            }
            else
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Env - ConsoleOutputLogLevel: <not specified> ({logLevel})");
                Console.WriteLine($"Console log is disabled");
                Console.ForegroundColor = color;
            }
        }

        private static void SetupSeq(IConfigurationRoot configRoot, LoggerConfiguration config, string seqServiceUrl)
        {
            if (!string.IsNullOrEmpty(seqServiceUrl))
            {
                config.WriteTo.Seq(seqServiceUrl, period: TimeSpan.FromSeconds(1));
            }
            else
            {
                Console.WriteLine("START WITHOUT SEQ LOGS");
            }
        }

        private sealed class ElasticsearchUrlsConfig
        {
            public IReadOnlyCollection<string> NodeUrls { get; set; }
            public string IndexPrefixName { get; set; }
        }

        private sealed class ElasticsearchConfig
        {
            public ElasticsearchUrlsConfig ElasticsearchLogs { get; set; }
        }

    }
}