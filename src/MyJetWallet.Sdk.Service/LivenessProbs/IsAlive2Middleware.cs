using System;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service.LivnesProbs;

// ReSharper disable InconsistentNaming

namespace MyJetWallet.Sdk.Service.LivenessProbs;

public class IsAlive2Middleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LivnessMiddleware> _logger;
    private readonly LivenessManager _manager;

    private static string _isAliveData = String.Empty;

    public IsAlive2Middleware(RequestDelegate next, 
        ILogger<LivnessMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path == "/api/isalive2")
        {
            if (string.IsNullOrEmpty(_isAliveData))
            {
                var data = new IsAliveData
                {
                    name = ApplicationEnvironment.AppName,
                    env_info = ApplicationEnvironment.HostName,
                    version = ApplicationEnvironment.AppVersion,
                    started = ApplicationEnvironment.StartedAt.UnixTime()
                };
                _isAliveData = data.ToJson();
            }
            
            context.Response.StatusCode = 200;
            await context.Response.WriteAsync(_isAliveData);
        }
        else
        {
            await _next(context);
        }
    }

    private class IsAliveData
    {
        public string name { get; set; }
        public string version { get; set; }
        public string env_info { get; set; }
        public long started { get; set; }
        
    }
}