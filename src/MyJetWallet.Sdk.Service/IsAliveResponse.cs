using Newtonsoft.Json;

namespace MyJetWallet.Sdk.Service
{
    public static class IsAliveResponse
    {
        public static string IsAlive()
        {
            return JsonConvert.SerializeObject(new
            {
                ApplicationEnvironment.AppName,
                ApplicationEnvironment.AppVersion,
                ApplicationEnvironment.Environment,
                ApplicationEnvironment.HostName,
                ApplicationEnvironment.UserName,
                ApplicationEnvironment.StartedAt
            });
        }
    }
}