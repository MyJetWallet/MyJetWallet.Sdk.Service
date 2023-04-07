using System.Collections.Generic;

namespace MyJetWallet.Sdk.Service.ServiceStatusReporter;

public class DependenciesMapApiModel
{
    public Dictionary<string, string> DependenciesMap { get; set; }

    public static DependenciesMapApiModel Create(Dictionary<string, string> map) => new DependenciesMapApiModel
    {
        DependenciesMap = map
    };
}