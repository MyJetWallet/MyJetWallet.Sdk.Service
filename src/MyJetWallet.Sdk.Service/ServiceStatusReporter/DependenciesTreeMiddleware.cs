using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MyJetWallet.Sdk.Service.ServiceStatusReporter;

public class DependenciesTreeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Assembly _appAssembly;
    private const string SystemLibsPattern = "System";

    public DependenciesTreeMiddleware(RequestDelegate next, Assembly appAssembly)
    {
        _next = next;
        _appAssembly = appAssembly;
    }

    public async Task InvokeAsync(HttpContext context) =>
        await context.Response.WriteAsync(JsonSerializer.Serialize(GetDependenciesTreeModel()));

    private DependenciesMapApiModel GetDependenciesTreeModel() => DependenciesMapApiModel.Create(_appAssembly
        .GetReferencedAssemblies()
        .Where((Func<AssemblyName, bool>) (itm => itm.Name != null && !itm.Name.Contains("System"))).ToDictionary(
            (Func<AssemblyName, string>) (itm => itm.Name),
            (Func<AssemblyName, string>) (itm => itm.Version?.ToString())));
}