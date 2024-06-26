﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
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
        public static readonly ActivitySource Source;

        static MyTelemetry()
        {
            Source = new ActivitySource(ApplicationEnvironment.AppName);

            ActivitySource.AddActivityListener(new ActivityListener
            {
                ShouldListenTo = s => true,
                SampleUsingParentId = (ref ActivityCreationOptions<string> activityOptions) => ActivitySamplingResult.AllData,
                Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) => ActivitySamplingResult.AllData
            });
        }

        public static IServiceCollection AddMyTelemetry(this IServiceCollection services,
            string appNamePrefix,
            string zipkinEndpoint = null,
            Func<HttpRequest, bool> httpRequestFilter = null,
            IEnumerable<string> sources = null,
            bool errorStatusOnException = false,
            bool setDbStatementForText = true)
        {
            services
                .AddOpenTelemetry()
                .WithTracing(builder =>
                {
                    builder
                        .AddAspNetCoreInstrumentation(options =>
                        {
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
                        })
                        .AddGrpcClientInstrumentation()
                        .AddEntityFrameworkCoreInstrumentation(option =>
                        {
                            option.SetDbStatementForText = setDbStatementForText;
                        })
                        .SetSampler(new AlwaysOnSampler())
                        .AddSource(ApplicationEnvironment.AppName)
                        .AddSource("MyJetWallet")
                        .AddGrpcClientInstrumentation()
                        .AddProcessor(new MyExceptionProcessor())
                        .AddProcessor(new MySpanTraceProcessor())
                        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService($"{appNamePrefix}{ApplicationEnvironment.AppName}"));

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

                        Console.WriteLine("Telemetry to Zipkin - ACTIVE");
                    }
                    else
                    {
                        Console.WriteLine("Telemetry to Zipkin - DISABLED");
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
            activity.SetStatus(Status.Error);

            return activity;
        }

        [CanBeNull]
        public static Activity WriteToActivity(this Exception ex)
        {
            var activity = Activity.Current;

            if (activity == null) return activity;

            activity.RecordException(ex);
            

            return activity;
        }

        [CanBeNull]
        public static Activity AddToActivityAsJsonTag(this object obj, string tag)
        {
            var activity = Activity.Current;
            activity?.AddTag(tag, JsonSerializer.Serialize(obj));
            return activity;
        }

        [CanBeNull]
        public static Activity AddToActivityAsTag(this object obj, string tag)
        {
            var activity = Activity.Current;
            activity?.AddTag(tag, obj);
            return activity;
        }


    }
}