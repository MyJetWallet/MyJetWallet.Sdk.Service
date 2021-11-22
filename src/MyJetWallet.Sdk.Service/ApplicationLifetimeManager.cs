using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using MyJetWallet.Sdk.Service.LivnesProbs;

namespace MyJetWallet.Sdk.Service
{
    [UsedImplicitly]
    public class ApplicationLifetimeManagerBase : IHostedService
    {
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly LivenessManager _livenessManager;

        public ApplicationLifetimeManagerBase(IHostApplicationLifetime appLifetime, LivenessManager livenessManager)
        {
            _appLifetime = appLifetime;
            _livenessManager = livenessManager;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _appLifetime.ApplicationStarted.Register(OnStarted);
            _appLifetime.ApplicationStopping.Register(OnStopping);
            _appLifetime.ApplicationStopped.Register(OnStopped);
            
            _livenessManager.Start();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected virtual void OnStarted()
        {
        }

        protected virtual void OnStopping()
        {
        }

        protected virtual void OnStopped()
        {
        }
    }
}