using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Teraa.Twitch.PubSub.Messages.LowTrustUsers;

public static class Parser
{
    private static readonly JsonSerializerOptions s_serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public static bool TryParse(JsonDocument input, [NotNullWhen(true)] out TreatmentUpdate? result)
    {
        if (!input.RootElement.TryGetProperty("type", out var typeElement) ||
            !string.Equals(typeElement.GetString(), "low_trust_user_treatment_update", StringComparison.Ordinal) ||
            !input.RootElement.TryGetProperty("data", out var data))
        {
            result = null;
            return false;
        }

        result = data.Deserialize<TreatmentUpdate>(s_serializerOptions);
        return result is not null;
    }
}
