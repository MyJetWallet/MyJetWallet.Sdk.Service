using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using MyJetWallet.Sdk.Service.ServiceStatusReporter;

namespace MyJetWallet.Sdk.Service;

public static class ApplicationBuilderUtils
{
    public static void BindIsAliveEndpoint(this IApplicationBuilder app) => app.Map((PathString) "/api/isalive", (Action<IApplicationBuilder>) (builder => builder.UseMiddleware<IsAliveMiddleware>(new Dictionary<string, string>())));

    public static void BindIsAliveEndpoint(this IApplicationBuilder app, IDictionary<string, string> envVariables)
    {
        app.Map((PathString) "/api/isalive", (Action<IApplicationBuilder>) (builder => builder.UseMiddleware<IsAliveMiddleware>(envVariables)));
    }

    public static void BindDependenciesTree(this IApplicationBuilder app, Assembly appAssembly) => app.Map((PathString) "/api/dependencies", (Action<IApplicationBuilder>) (builder => builder.UseMiddleware<DependenciesTreeMiddleware>(appAssembly)));
    
    public static void BindGrpcMetrics(this IApplicationBuilder app) => app.Map((PathString) "/api/grpcmetrics", (Action<IApplicationBuilder>) (builder => builder.UseMiddleware<GrpcMetricsMiddleware>()));

}