using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using MyJetWallet.Sdk.GrpcMetrics;

namespace MyJetWallet.Sdk.Service.ServiceStatusReporter;

public class GrpcMetricsMiddleware
{
    private readonly RequestDelegate _next;

    public GrpcMetricsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context) =>
        await context.Response.WriteAsync(JsonSerializer.Serialize(GetMetrics()));

    private MetricsModel GetMetrics()
    {
        return new MetricsModel
        {
            ServerMetrics = GrpcMetricsManager.ServiceMetrics,
            ClientMetrics = GrpcMetricsManager.ClientMetrics
        };
    }


    private class  MetricsModel
    {
        public Dictionary<string, Metric> ServerMetrics { get; set; }
        public Dictionary<string, Metric> ClientMetrics { get; set; }
    }
}