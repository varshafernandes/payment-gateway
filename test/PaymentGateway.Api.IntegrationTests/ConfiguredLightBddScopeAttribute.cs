using LightBDD.Core.Configuration;
using LightBDD.XUnit2;

using PaymentGateway.Api.IntegrationTests;

[assembly: ConfiguredLightBddScope]

namespace PaymentGateway.Api.IntegrationTests;

public sealed class ConfiguredLightBddScopeAttribute : LightBddScopeAttribute
{
    protected override void OnConfigure(LightBddConfiguration configuration)
    {
        
    }
}
