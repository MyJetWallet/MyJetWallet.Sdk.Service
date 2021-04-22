using System;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;

namespace MyJetWallet.Sdk.Service
{
    [UsedImplicitly]
    public static class ApplicationEnvironment
    {
        static ApplicationEnvironment()
        {
            var configBuilder = new ConfigurationBuilder().AddEnvironmentVariables();

            Config = configBuilder.Build();
            Environment = Config["ASPNETCORE_ENVIRONMENT"];
            HostName = Config["HOSTNAME"];
            UserName = System.Environment.UserName;

            StartedAt = DateTime.UtcNow;

            var name = Assembly.GetEntryAssembly()?.GetName();

            string appName = name?.Name ?? string.Empty;

            var nameSegments = appName.Split('.', StringSplitOptions.RemoveEmptyEntries);

            if (nameSegments.Length > 2)
            {
                appName = string.Join('.', nameSegments.Skip(1));
            }

            AppName = appName;
            AppVersion = name?.Version?.ToString();

            EnvInfo = Config["ENV_INFO"];
        }

        /// <summary>
        /// This config includes only settings provided via environment variables.
        /// Intended to read setting from the environment until main app configuration is built.
        /// </summary>
        public static IConfigurationRoot Config { get; }

        public static string Environment { get; }

        public static string HostName { get; }

        public static string UserName { get; }

        public static DateTime StartedAt { get; }

        public static string AppName { get; }

        public static string AppVersion { get; }

        public static string EnvInfo { get; }

        public static bool IsDevelopment => Environment == "Development";

        public static bool IsStaging => Environment == "Staging";

        public static bool IsProduction => Environment == "Production";
    }
}