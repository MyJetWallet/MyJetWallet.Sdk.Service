using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;

namespace MyJetWallet.Sdk.Service.Tools
{
    public class MyTaskTimer: IStartable, IDisposable
    {
        private readonly string _owner;
        private TimeSpan _interval;
        private readonly ILogger _logger;
        private readonly Func<Task> _doProcess;
        private readonly CancellationTokenSource _token = new();
        private Task _process;

        public MyTaskTimer(string owner, TimeSpan interval, ILogger logger, Func<Task> doProcess)
        {
            _owner = owner;
            _interval = interval;
            _logger = logger;
            _doProcess = doProcess;
        }
        
        public MyTaskTimer(Type owner, TimeSpan interval, ILogger logger, Func<Task> doProcess)
            :this (owner.Name, interval, logger, doProcess)
        { }

        public static MyTaskTimer Create<T>(TimeSpan interval, ILogger logger, Func<Task> doProcess)
        {
            return new MyTaskTimer(typeof(T), interval, logger, doProcess);
        }

        public void ChangeInterval(TimeSpan interval)
        {
            _interval = interval;
        }

        public void Start()
        {
            _process = Task.Run(DoProcessInt, _token.Token);
        }

        private async Task DoProcessInt()
        {
            _logger.LogInformation($"Timer '{_owner}' is started");
            try
            {
                while (!_token.IsCancellationRequested)
                {
                    using (var activity = MyTelemetry.StartActivity($"Timer:{_owner}"))
                    {
                        activity?.SetTag("MyTaskTimer", _owner);
                        activity?.SetTag("MyTaskTimer.interval", _interval.ToString());
                        try
                        {
                            await _doProcess();
                        }
                        catch (Exception ex)
                        {
                            ex.FailActivity();

                            _logger.LogError(ex, $"Unhandled exception from DoProcess in MyTaskTimer[{_owner}]");
                        }
                    }

                    await Task.Delay(_interval, _token.Token);
                }
            }
            catch (Exception)
            { }

            _logger.LogInformation($"Timer '{_owner}' is stopped");
        }

        public void Stop()
        {
            _token.Cancel();
            _process?.Wait();
        }

        public void Dispose()
        {
            Stop();
        }
    }
}