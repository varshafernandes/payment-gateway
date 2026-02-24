using LightBDD.Core.Configuration;
using LightBDD.XUnit2;

using PaymentGateway.Api.Tests;

[assembly: ConfiguredLightBddScope]

namespace PaymentGateway.Api.Tests;


public sealed class ConfiguredLightBddScopeAttribute : LightBddScopeAttribute
{
    protected override void OnConfigure(LightBddConfiguration configuration)
    {
    }
}
