using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MyJetWallet.Sdk.Service.ServiceStatusReporter;

public class IsAliveMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDictionary<string, string> _envVariables;

    public IsAliveMiddleware(RequestDelegate next, IDictionary<string, string> envVariables)
    {
        _next = next;
        _envVariables = envVariables;
    }

    public async Task InvokeAsync(HttpContext context) => await context.Response.WriteAsync(JsonSerializer.Serialize(GetIsAliveApiModel()));

    private IsAliveApiModel GetIsAliveApiModel()
    {
        string environmentVariable1 = Environment.GetEnvironmentVariable("APP_VERSION");
        string environmentVariable2 = Environment.GetEnvironmentVariable("APP_COMPILATION_DATE");
        Version version = Environment.Version;
        if (string.IsNullOrEmpty(environmentVariable1))
            throw new ArgumentNullException("Enviroment variable APP_VERSION null or empty");
        if (string.IsNullOrEmpty(environmentVariable2))
            throw new ArgumentNullException("Enviroment variable APP_COMPILATION_DATE null or empty");
        return new IsAliveApiModel
        {
            IsAlive = true,
            AppVersion = environmentVariable1,
            AppCompilationDate = Convert.ToDateTime(environmentVariable2),
            EnvInfo = Environment.GetEnvironmentVariable("ENV_INFO") ?? "NO_INFO",
            EnvVariablesSha1 = _envVariables,
            FrameworkVersion = version.ToString()
        };
    }
}