using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Refit;
using Serilog;
using Teraa.Twitch.Helix;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateLogger();

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

var services = new ServiceCollection()
    .AddLogging(configure => configure.AddSerilog())
    .AddHelixApi<HelixApiAuthProvider>()
    .Services
    .AddSingleton<IConfiguration>(config)
    .BuildServiceProvider(new ServiceProviderOptions
    {
        ValidateScopes = true,
        ValidateOnBuild = true,
    });

var api = services.GetRequiredService<IHelixApi>();
var user = await api.GetUser(login: ["twitch", "justin"]);
return;

internal class HelixApiAuthProvider : IHelixApiAuthProvider
{
    private readonly IConfiguration _configuration;

    public HelixApiAuthProvider(IConfiguration config, IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public ValueTask<string> GetClientIdAsync(CancellationToken cancellationToken)
    {
        var clientId = _configuration.GetRequiredSection("Twitch:ClientId").Value!;
        return ValueTask.FromResult(clientId);
    }

    public ValueTask<AuthenticationHeaderValue> GetAuthHeader(CancellationToken cancellationToken)
    {
        var token = _configuration.GetRequiredSection("Twitch:Token").Value;
        var header = new AuthenticationHeaderValue("Bearer", token);
        return ValueTask.FromResult(header);
    }
}
