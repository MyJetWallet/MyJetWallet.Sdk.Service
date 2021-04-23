using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace MyJetWallet.Sdk.Service
{
    public static class MyLoggerFactoryDatabaseWrapper
    {
        public static ILoggerFactory ToDatabaseLogger(this ILoggerFactory factory)
        {
            return new LogFactoryDatabaseWrapper(factory);
        }

        public static ILogger ToDatabaseLogger(this ILogger logger)
        {
            return new LoggerDatabaseWrapper(logger);
        }

    }

    public class LogFactoryDatabaseWrapper : ILoggerFactory
    {
        private readonly ILoggerFactory _factory;

        public LogFactoryDatabaseWrapper(ILoggerFactory factory)
        {
            _factory = factory;
        }

        public void Dispose()
        {
            _factory.Dispose();
        }

        public void AddProvider(ILoggerProvider provider)
        {
            _factory.AddProvider(provider);
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _factory.CreateLogger(categoryName).ToDatabaseLogger();
        }
    }


    public class LoggerDatabaseWrapper : ILogger
    {
        private readonly ILogger _logger;

        public LoggerDatabaseWrapper(ILogger logger)
        {
            _logger = logger;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return _logger.BeginScope(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _logger.IsEnabled(logLevel);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (logLevel == LogLevel.Critical || logLevel == LogLevel.Error)
            {
                logLevel = LogLevel.Information;
            }
            _logger.Log<TState>(logLevel, eventId, state, exception, formatter);
        }
    }
}