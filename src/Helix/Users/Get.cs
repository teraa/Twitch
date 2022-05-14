using FluentValidation;
using JetBrains.Annotations;
using MediatR;

namespace Teraa.Twitch.Helix.Users;

public static class Get
{
    [PublicAPI]
    public record Query(
        IReadOnlyList<string>? Ids,
        IReadOnlyList<string>? Logins)
        : IRequest<Response>;

    [UsedImplicitly]
    public class QueryValidator : AbstractValidator<Query>
    {
        public QueryValidator()
        {
            RuleFor(x => x)
                .Must(x => (x.Ids?.Count ?? 0 + x.Logins?.Count ?? 0) is > 0 and <= 100)
                .WithMessage($"Combined count of {nameof(Query.Ids)} and {nameof(Query.Logins)} must be in range [1, 100]");
        }
    }

    [PublicAPI]
    public record Response(
        IReadOnlyList<ResponseData> Data);

    [PublicAPI]
    public record ResponseData(
        string Id,
        string Login,
        string DisplayName,
        string Type,
        string BroadcasterType,
        string Description,
        string ProfileImageUrl,
        string OfflineImageUrl,
        DateTimeOffset CreatedAt,
        string? Email);

    [UsedImplicitly]
    public class Handler : IRequestHandler<Query, Response>
    {
        public Task<Response> Handle(Query request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}

