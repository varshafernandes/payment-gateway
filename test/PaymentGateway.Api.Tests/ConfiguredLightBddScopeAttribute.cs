using LightBDD.Core.Configuration;
using LightBDD.XUnit2;

using PaymentGateway.Api.Tests;

// Required by LightBDD to hook into xUnit test lifecycle.
[assembly: ConfiguredLightBddScope]

namespace PaymentGateway.Api.Tests;

/// <summary>
/// Configures the LightBDD test scope for xUnit. 
/// Add custom configuration in OnConfigure if needed.
/// </summary>
public sealed class ConfiguredLightBddScopeAttribute : LightBddScopeAttribute
{
    protected override void OnConfigure(LightBddConfiguration configuration)
    {
        // Default configuration is sufficient for our needs
    }
}
