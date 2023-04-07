using System;
using System.Collections.Generic;

namespace MyJetWallet.Sdk.Service.ServiceStatusReporter;

public class IsAliveApiModel
{
    public bool IsAlive { get; set; }

    public string? FrameworkVersion { get; set; }

    public string? AppVersion { get; set; }

    public DateTime AppCompilationDate { get; set; }

    public string? EnvInfo { get; set; }

    public IDictionary<string, string> EnvVariablesSha1 { get; set; }
}