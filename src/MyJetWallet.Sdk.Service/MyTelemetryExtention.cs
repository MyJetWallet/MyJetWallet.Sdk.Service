using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
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
            bool setDbStatementForText = true,
            string serviceName = default,
            OtlpSettings otlpSettings = null)
        {
            if (!string.IsNullOrEmpty(otlpSettings?.OtlpApiKey))
            {
                services.Configure<OtlpExporterOptions>(o =>
                    o.Headers = $"{OtlpSettings.ApiKeyHeaderName}={otlpSettings.OtlpApiKey}");
            }
            var openTelementryBuilder = services
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
                        .SetSampler<AlwaysOnSampler>()
                        .AddSource(ApplicationEnvironment.AppName)
                        .AddSource("MyJetWallet")
                        .AddGrpcClientInstrumentation()
                        .AddProcessor<MyExceptionProcessor>()
                        .AddProcessor<MySpanTraceProcessor>()
                        .SetResourceBuilder(GetResourceBuilder(serviceName ?? $"{appNamePrefix}{ApplicationEnvironment.AppName}"));

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

                        Console.WriteLine("Traces telemetry to Zipkin - ACTIVE");
                    }
                    else
                    {
                        Console.WriteLine("Traces telemetry to Zipkin - DISABLED");
                    }

                    if (!string.IsNullOrEmpty(otlpSettings?.OtlpEndpoint))
                    {
                        builder.AddOtlpExporter(options =>
                        {
                            options.Endpoint = new Uri(otlpSettings.OtlpEndpoint);
                            options.ExportProcessorType = ExportProcessorType.Batch;
                        });

                        Console.WriteLine("Traces telemetry to OpenTelemetryProtocol - ACTIVE");
                    }
                    else
                    {
                        Console.WriteLine("Traces telemetry to OpenTelemetryProtocol - DISABLED");
                    }
                })
              .WithMetrics(builder =>
              {
                  builder
                    .SetResourceBuilder(GetResourceBuilder(serviceName ?? $"{appNamePrefix}{ApplicationEnvironment.AppName}"))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
                  if (!string.IsNullOrEmpty(otlpSettings?.OtlpEndpoint))
                  {
                      builder.AddOtlpExporter(options =>
                      {
                          options.Endpoint = new Uri(otlpSettings.OtlpEndpoint);
                          options.ExportProcessorType = ExportProcessorType.Batch;
                      });

                      Console.WriteLine("Metrics telemetry to OpenTelemetryProtocol - ACTIVE");
                  }
                  else
                  {
                      Console.WriteLine("Metrics telemetry to OpenTelemetryProtocol - DISABLED");
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

        public static ResourceBuilder GetResourceBuilder(
        string serviceName = null,
        string serviceNamespace = null,
        string serviceVersion = null,
        bool autoGenerateServiceInstanceId = true,
        string serviceInstanceId = null)
        {
            serviceName ??= ApplicationEnvironment.AppName;
            serviceNamespace ??= ApplicationEnvironment.Environment;
            serviceVersion ??= ApplicationEnvironment.AppVersion;

            return ResourceBuilder
                .CreateDefault()
                .AddService(
                    serviceName,
                    serviceNamespace,
                    serviceVersion,
                    autoGenerateServiceInstanceId,
                    serviceInstanceId);
        }
    }
}