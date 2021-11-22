using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service.LivnesProbs;

namespace MyJetWallet.Sdk.Service.LivenessProbs
{
    public class LivnessMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LivnessMiddleware> _logger;
        private readonly LivenessManager _manager;

        public LivnessMiddleware(RequestDelegate next, ILogger<LivnessMiddleware> logger, LivenessManager manager)
        {
            _next = next;
            _logger = logger;
            _manager = manager;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path == "/api/livness")
            {
                var issues = _manager.Issues;

                if (issues.Any())
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync(issues.ToJson());
                }
                else
                {
                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsync("{\"status\":\"ok\"}");
                }
            }
            else
            {
                await _next(context);
            }
        }
    }
}