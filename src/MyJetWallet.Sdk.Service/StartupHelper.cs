using System;
using Grpc.AspNetCore.Server;
using Microsoft.Extensions.DependencyInjection;
using MyJetWallet.Sdk.Grpc;
using MyJetWallet.Sdk.GrpcMetrics;
using ProtoBuf.Grpc.Server;

namespace MyJetWallet.Sdk.Service
{
    public static class StartupHelper
    {
        public static IServiceCollection BindCodeFirstGrpc(
            IServiceCollection services,
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
    }
}