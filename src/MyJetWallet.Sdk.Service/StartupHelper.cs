using System;
using System.Linq;
using System.Reflection;
using Autofac;
using Grpc.AspNetCore.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyJetWallet.Sdk.Grpc;
using MyJetWallet.Sdk.GrpcMetrics;
using MyJetWallet.Sdk.GrpcSchema;
using MyJetWallet.Sdk.Service.LivnesProbs;
using Prometheus;
using ProtoBuf.Grpc.Server;
using SimpleTrading.ServiceStatusReporterConnector;

namespace MyJetWallet.Sdk.Service
{
    public static class StartupHelper
    {
        public static IServiceCollection BindCodeFirstGrpc(
            this IServiceCollection services,
            Action<GrpcServiceOptions>? configureOptions = null)
        {
            services.AddCodeFirstGrpc(options =>
            {
                options.Interceptors.Add<PrometheusMetricsInterceptor>();
                options.Interceptors.Add<CallSourceInterceptor>();
                options.Interceptors.Add<ExceptionInterceptor>();
                
                configureOptions?.Invoke(options);
            });

            return services;
        }

        public static void ConfigureJetWallet<TLifetimeManager>(this IServiceCollection services, string zipkinUrl,
            string appNamePrefix = "SP-")
        where TLifetimeManager: class, IHostedService
        {
            services.BindCodeFirstGrpc();
            services.AddHostedService<TLifetimeManager>();
            services.AddMyTelemetry(appNamePrefix, zipkinUrl);
        }
        
        public static void ConfigureJetWallet(this IApplicationBuilder app, IWebHostEnvironment env,
            Action<IEndpointRouteBuilder> configureGrpcServices)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseMetricServer();

            app.BindServicesTree(Assembly.GetExecutingAssembly());

            app.BindIsAlive();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcSchemaRegistry();
                
                configureGrpcServices?.Invoke(endpoints);

                endpoints.MapGet("/api/livness", async context =>
                {
                    var issues = LivenessManager.Instance?.Issues;

                    if (issues == null)
                    {
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync("{ \"status\": \"LivenessManager does not exist\"}");
                    }
                    
                    if (issues.Any())
                    {
                        context.Response.StatusCode = 500;
                        await context.Response.WriteAsync(issues.ToJson());
                    }
                    
                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsync("{ \"status\": \"ok\"}");
                });
                
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });
        }

        public static void ConfigureJetWallet(this ContainerBuilder builder)
        {
            builder.RegisterType<LivenessManager>().AsSelf().AutoActivate().SingleInstance();
        }
    }
}