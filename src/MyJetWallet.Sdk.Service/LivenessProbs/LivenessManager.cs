using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service.Tools;
using Newtonsoft.Json;

namespace MyJetWallet.Sdk.Service.LivnesProbs
{
    public class LivenessManager
    {
        private readonly ILivenessReporter[] _reporters;
        private readonly ILogger<LivenessManager> _logger;
        private MyTaskTimer _timer;
        public Dictionary<string, List<string>> Issues { get; private set; }
        
        public static LivenessManager Instance { get; private set; }
        

        public LivenessManager(ILivenessReporter[] reporters, ILogger<LivenessManager> logger)
        {
            Issues = new Dictionary<string, List<string>>()
            {
                {
                    "LivenessManager", new List<string>()
                    {
                        "LivenessManager does not started"
                    }
                }
            };
            
            _reporters = reporters;
            _logger = logger;
            _timer = new MyTaskTimer(nameof(LivenessManager), TimeSpan.FromSeconds(30), logger, DoTime);
            Instance = this;
            
            Console.WriteLine("Count reports: {count}", reporters?.Length);
        }

        private Task DoTime()
        {
            var list = new Dictionary<string, List<string>>();
            foreach (var reporter in _reporters)
            {
                var (service, issues) = reporter.GetIssues();

                if (string.IsNullOrEmpty(service))
                    service = reporter.GetType().Name;

                if (issues.Any())
                {
                    _logger.LogError("Detect Issues from service {name}, list of issues: {jsonText}", service, 
                        JsonConvert.SerializeObject(issues));

                    list[service] = issues;
                }
            }

            Issues = list;
            
            return Task.CompletedTask;
        }

        public void Start()
        {
            Console.WriteLine("-- Start LivenessManager --");
            _timer.Start();
        }
    }

    public interface ILivenessReporter
    {
        (string, List<string>) GetIssues();
    }
}