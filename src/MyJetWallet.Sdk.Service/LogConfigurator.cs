using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Elasticsearch.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

namespace MyJetWallet.Sdk.Service
{
    public static class LogConfigurator
    {
        public static ILoggerFactory ConfigureElk(
            string productName = default,
            string seqServiceUrl = default,
            LogElkSettings logElkSettings = null)
        {
            Console.WriteLine($"App - name: {ApplicationEnvironment.AppName}");
            Console.WriteLine($"App - version: {ApplicationEnvironment.AppVersion}");

            IConfigurationRoot configRoot = BuildConfigRoot();

            var config = new LoggerConfiguration()
                
                .ReadFrom.Configuration(configRoot)
                .Enrich.FromLogContext()
                .Enrich.With<ActivityEnricher>()
                .Enrich.WithExceptionData()
                .Enrich.WithCorrelationIdHeader();

            SetupProperty(productName, config);

            SetupConsole(configRoot, config);

            SetupSeq(configRoot, config, seqServiceUrl);

            SetupElk(logElkSettings, config);

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

        private static void SetupElk(LogElkSettings logElkSettings, LoggerConfiguration config)
        {
            var prefix = !string.IsNullOrEmpty(logElkSettings.IndexPrefix) ? logElkSettings.IndexPrefix : "jet-logs-def";

            if (logElkSettings?.Urls?.Any() == true)
            {
                var number = 0;

                if (logElkSettings?.Urls.Count > 1)
                {
                    var rnd = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);
                    number = rnd.Next(logElkSettings.Urls.Count);
                }

                var url = logElkSettings.Urls.Values.ToArray()[number];

                config.WriteTo.Elasticsearch(
                    new ElasticsearchSinkOptions(new Uri(url))
                    {
                        AutoRegisterTemplate = true,
                        EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog,
                        AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
                        IndexDecider = (e, o) => $"{prefix}-{o.Date:yyyy-MM-dd}",
                        ModifyConnectionSettings = configuration =>
                        {
                            configuration.ServerCertificateValidationCallback(CertificateValidations.AllowAll);

                            if (!string.IsNullOrEmpty(logElkSettings.User))
                            {
                                configuration.BasicAuthentication(logElkSettings.User, logElkSettings.Password);
                            }

                            return configuration;
                        }
                    });

                Console.WriteLine($"SETUP LOGGING TO ElasticSearch. Url Number: {number} ({url}). Index name: {prefix}-yyyy-MM-dd");
            }
        }

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


    public class ActivityEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var activity = Activity.Current;

            if (activity != null)
            {
                logEvent.AddPropertyIfAbsent(new LogEventProperty("Span_Id", new ScalarValue(activity.GetSpanId())));
                logEvent.AddPropertyIfAbsent(new LogEventProperty("Trace_Id", new ScalarValue(activity.GetTraceId())));
                logEvent.AddPropertyIfAbsent(new LogEventProperty("Parent_Id", new ScalarValue(activity.GetParentId())));
            }
        }
    }

    internal static class ActivityExtensions
    {
        public static string GetSpanId(this Activity activity)
        {
            return activity.IdFormat switch
            {
                ActivityIdFormat.Hierarchical => activity.Id,
                ActivityIdFormat.W3C => activity.SpanId.ToHexString(),
                _ => null,
            } ?? string.Empty;
        }

        public static string GetTraceId(this Activity activity)
        {
            return activity.IdFormat switch
            {
                ActivityIdFormat.Hierarchical => activity.RootId,
                ActivityIdFormat.W3C => activity.TraceId.ToHexString(),
                _ => null,
            } ?? string.Empty;
        }

        public static string GetParentId(this Activity activity)
        {
            return activity.IdFormat switch
            {
                ActivityIdFormat.Hierarchical => activity.ParentId,
                ActivityIdFormat.W3C => activity.ParentSpanId.ToHexString(),
                _ => null,
            } ?? string.Empty;
        }
    }
}