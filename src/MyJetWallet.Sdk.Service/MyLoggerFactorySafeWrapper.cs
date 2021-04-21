using System;
using Microsoft.Extensions.Logging;

namespace MyJetWallet.Sdk.Service
{
    public static class MyLoggerFactorySafeWrapper
    {
        public static ILoggerFactory ToSafeLogger(this ILoggerFactory factory)
        {
            return new LogFactorySafeWrapper(factory);
        }

        public static ILogger ToSaveLogger(this ILogger logger)
        {
            return new LoggerSafeWrapper(logger);
        }

    }

    public class LogFactorySafeWrapper : ILoggerFactory
    {
        private readonly ILoggerFactory _factory;

        public LogFactorySafeWrapper(ILoggerFactory factory)
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
            return _factory.CreateLogger(categoryName).ToSaveLogger();
        }
    }


    public class LoggerSafeWrapper : ILogger
    {
        private readonly ILogger _logger;

        public LoggerSafeWrapper(ILogger logger)
        {
            _logger = logger;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return _logger.BeginScope(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            try
            {
                return _logger.IsEnabled(logLevel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"!!!!!!!!!!!!!!!!!\n[ERROR] ILogger.IsEnabled receive exception\n{ex}");
                return false;
            }
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            try
            {
                _logger.Log<TState>(logLevel, eventId, state, exception, formatter);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"!!!!!!!!!!!!!!!!!\n[ERROR] ILogger.Log receive exception\n{ex}");
                LogToConsole(logLevel, eventId, state, exception, formatter);
            }

        }

        private void LogToConsole<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            try
            {
                var text = formatter(state, exception);
                Console.WriteLine($"[{logLevel}] ({eventId.ToString()}) {text}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"!!!!!!!!!!!!!!!!!\n[ERROR] Cannot execute formatter() in ILogger.Log\n{e}");
            }
        }
    }
}