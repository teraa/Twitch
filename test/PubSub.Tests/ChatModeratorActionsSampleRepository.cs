using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Xunit;

namespace PubSub.Tests;

[UsedImplicitly]
public class ChatModeratorActionsSampleRepository : IAsyncLifetime
{
    private readonly Dictionary<string, string> _samples;

    public ChatModeratorActionsSampleRepository()
    {
        _samples = new Dictionary<string, string>();
    }

    public IReadOnlyDictionary<string, string> Samples => _samples;

    public async Task InitializeAsync()
    {
        const string prefix = "PubSub.Tests.Payloads.";

        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly
            .GetManifestResourceNames()
            .Where(x => x.StartsWith(prefix));

        foreach (var resourceName in resourceNames)
        {
            await using var stream = assembly.GetManifestResourceStream(resourceName);

            if (stream is null)
                continue;

            using var sr = new StreamReader(stream);
            var json = await sr.ReadToEndAsync();

            _samples[resourceName[prefix.Length..]] = json;
        }
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
