![Nuget version](https://img.shields.io/nuget/v/MyJetWallet.Sdk.Service?label=MyJetWallet.Sdk.Service&style=social)

# Telemetry


registration:
```
public class Startup
{
	public void ConfigureServices(IServiceCollection services)
	{
		services.AddMyTelemetry("http://localhost:9411/api/v2/spans");
	}

}
```

using:
```

	using var activity = MyTelemetry.StartActivity("new action");

	activity?.
	activity?.SetTag("Client-Source", "Test.App.Client");
	activity?.SetTag("service.tag", "Test.App.Client");
```

use new source:
```
private static readonly ActivitySource MyActivitySource = new ActivitySource("MyTestSource");

...

services.AddMyTelemetry("http://localhost:9411/api/v2/spans", sources: new[] {"MyTestSource"});

using var activity = MyActivitySource.StartActivity("Call Hello");

activity?.SetTag("Client-Source", "Test.App.Client");
```

handle exceptions

```
try
{
    ...
}
catch(Exception ex)
{
	ex.FailActivity();
	throw;
}
```
