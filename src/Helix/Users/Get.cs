using System.Text.Json.Serialization;
using FluentValidation;
using JetBrains.Annotations;
using MediatR;

namespace Teraa.Twitch.Helix.Users;

public static class Get
{
    [PublicAPI]
    public record Query(
        string Token,
        IReadOnlyList<string>? Ids = null,
        IReadOnlyList<string>? Logins = null
    ) : IRequest<Response>;

    [UsedImplicitly]
    public class QueryValidator : AbstractValidator<Query>
    {
        public QueryValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty();

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
        [property: JsonPropertyName("id")]
        string Id,
        [property: JsonPropertyName("login")]
        string Login,
        [property: JsonPropertyName("display_name")]
        string DisplayName,
        [property: JsonPropertyName("type")]
        string Type,
        [property: JsonPropertyName("broadcaster_type")]
        string BroadcasterType,
        [property: JsonPropertyName("description")]
        string Description,
        [property: JsonPropertyName("profile_image_url")]
        string ProfileImageUrl,
        [property: JsonPropertyName("offline_image_url")]
        string OfflineImageUrl,
        [property: JsonPropertyName("created_at")]
        DateTimeOffset CreatedAt,
        [property: JsonPropertyName("email")]
        string? Email);

    [UsedImplicitly]
    public class Handler : IRequestHandler<Query, Response>
    {
        private readonly IMediator _mediator;

        public Handler(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
        {
            var response = await _mediator.Send(new Send.Request<Response>(
                HttpMethod: HttpMethod.Get,
                Path: "users",
                QueryBuilderOptions: queryBuilder =>
                {
                    if (request.Ids is { })
                        queryBuilder.Add("id", request.Ids);

                    if (request.Logins is { })
                        queryBuilder.Add("login", request.Logins);
                },
                Token: request.Token,
                RequestOptions: null
            ), cancellationToken);

            return response;
        }
    }
}

