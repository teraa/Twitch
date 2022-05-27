using System.Collections.Generic;
using Xunit;

namespace PubSub.Tests;

public class ChatModeratorActionsTests : IClassFixture<ChatModeratorActionsSampleRepository>
{
    private readonly IReadOnlyDictionary<string, string> _samples;

    public ChatModeratorActionsTests(ChatModeratorActionsSampleRepository sampleRepository)
    {
        _samples = sampleRepository.Samples;
    }
}
