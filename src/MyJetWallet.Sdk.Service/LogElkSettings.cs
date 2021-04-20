using System.Collections.Generic;
using MyYamlParser;

namespace MyJetWallet.Sdk.Service
{
    public class LogElkSettings
    {
        [YamlProperty("Urls")]
        public Dictionary<string, string> Urls { get; set; }

        [YamlProperty("IndexPrefix")]
        public string IndexPrefix { get; set; }

        [YamlProperty("User")]
        public string User { get; set; }

        [YamlProperty("Password")]
        public string Password { get; set; }
    }
}