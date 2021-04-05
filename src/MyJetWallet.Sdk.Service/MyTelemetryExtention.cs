using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MyJetWallet.Sdk.Service
{
    public static class MyTelemetry
    {
        public static readonly ActivitySource Source = new ActivitySource(ApplicationEnvironment.AppName);

        public static IServiceCollection AddMyTelemetry(this IServiceCollection services,
            string zipkinEndpoint = null,
            Func<HttpRequest, bool> httpRequestFilter = null,
            IEnumerable<string> sources = null,
            bool errorStatusOnException = false)
        {
            services.AddOpenTelemetryTracing((builder) =>
                {
                    builder
                        .AddAspNetCoreInstrumentation(options =>
                        {
                            Console.WriteLine(options.RecordException);
                            options.RecordException = true;
                            options.Filter = context =>
                            {
                                if (httpRequestFilter != null && httpRequestFilter(context.Request)) return false;
                                if (context.Request.Path.ToString().Contains("isalive")) return false;
                                if (context.Request.Path.ToString().Contains("metrics")) return false;
                                if (context.Request.Path.ToString().Contains("dependencies")) return false;
                                if (context.Request.Path.ToString().Contains("swagger")) return false;
                                if (context.Request.Path.ToString() == "/") return false;
                                return true;
                            };
                            options.EnableGrpcAspNetCoreSupport = true;
                        })
                        .SetSampler(new AlwaysOnSampler())
                        .AddSource(ApplicationEnvironment.AppName)
                        .AddGrpcClientInstrumentation()
                        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(ApplicationEnvironment.AppName));

                    if (errorStatusOnException)
                    {
                        builder.SetErrorStatusOnException();
                    }

                    if (sources != null)
                    {
                        foreach (var source in sources)
                        {
                            builder.AddSource(source);
                        }
                    }

                    if (!string.IsNullOrEmpty(zipkinEndpoint))
                    {
                        builder.AddZipkinExporter(options =>
                        {
                            options.Endpoint = new Uri(zipkinEndpoint);
                            options.UseShortTraceIds = true;
                            options.ExportProcessorType = ExportProcessorType.Batch;
                        });
                    }
                });

            return services;
        }

        [CanBeNull]
        public static Activity StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
        {
            return Source.StartActivity(name, kind);
        }

        [CanBeNull]
        public static Activity FailActivity(this Exception ex)
        {
            var activity = Activity.Current;
            
            if (activity == null) return activity;
            
            activity.RecordException(ex);
            activity.SetTag("exception", ex.ToString());
            activity.SetStatus(Status.Error);

            return activity;
        }


    }
}