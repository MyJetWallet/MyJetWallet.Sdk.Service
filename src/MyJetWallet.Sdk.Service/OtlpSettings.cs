using MyYamlParser;

namespace MyJetWallet.Sdk.Service;

public class OtlpSettings
{
    public const string ApiKeyHeaderName = "x-otlp-api-key";

    [YamlProperty("OtlpEndpoint")]
    public string OtlpEndpoint { get; set; }

    [YamlProperty("OtlpApiKey")]
    public string OtlpApiKey { get; set; }
}
