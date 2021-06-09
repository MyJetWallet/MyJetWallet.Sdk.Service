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

            OverrideLogLevel(config);

            SetupProperty(productName, config);

            SetupConsole(configRoot, config);

            SetupSeq(config, seqServiceUrl);

            SetupElk(logElkSettings, config);

            //Log.Logger = new SerilogSafeWrapper(config.CreateLogger());
            Log.Logger =config.CreateLogger();

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Log.Fatal((Exception)e.ExceptionObject, "Application has been terminated unexpectedly");
                Log.CloseAndFlush();
            };
            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                Log.CloseAndFlush();
            };

            return new LoggerFactory().AddSerilog().ToSafeLogger();
        }

        private static void OverrideLogLevel(LoggerConfiguration config)
        {
            config
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft.AspNetCore.Server.Kestrel", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore.Routing", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.StaticFiles.StaticFileMiddleware", LogEventLevel.Warning);
        }

        private static void SetupElk(LogElkSettings logElkSettings, LoggerConfiguration config)
        {
            if (logElkSettings?.Urls?.Any() == true)
            {
                var prefix = !string.IsNullOrEmpty(logElkSettings.IndexPrefix) ? logElkSettings.IndexPrefix : "jet-logs-def";

                var urls = logElkSettings.Urls.Values.Select(e => new Uri(e)).ToArray();

                config.WriteTo.Elasticsearch(
                    new ElasticsearchSinkOptions(urls)
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

                Console.WriteLine($"SETUP LOGGING TO ElasticSearch. Url Count: {urls.Length}. Index name: {prefix}-yyyy-MM-dd");
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

            SetupSeq(config, seqServiceUrl);

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

            return new LoggerFactory().AddSerilog().ToSafeLogger();
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

        private static void SetupSeq(LoggerConfiguration config, string seqServiceUrl)
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
                logEvent.AddPropertyIfAbsent(new LogEventProperty("Activity_Id", new ScalarValue(activity.GetActivityId())));
                logEvent.AddPropertyIfAbsent(new LogEventProperty("Activity_Duration", new ScalarValue(activity.GetActivityDuration())));
                
                foreach (var pair in activity.Baggage)
                {
                    logEvent.AddPropertyIfAbsent(new LogEventProperty($"bag-{pair.Key}", new ScalarValue(pair.Value)));
                }
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

        public static string GetActivityId(this Activity activity)
        {
            return activity.DisplayName;
        }

        public static TimeSpan GetActivityDuration(this Activity activity)
        {
            return activity.Duration;
        }
    }
}