using System.Text.Json.Serialization;
using FluentValidation;
using JetBrains.Annotations;
using MediatR;
using Microsoft.Extensions.Options;

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
        private readonly HelixServiceOptions _options;
        private readonly IMediator _mediator;

        public Handler(IOptions<HelixServiceOptions> options, IMediator mediator)
        {
            _mediator = mediator;
            _options = options.Value;
        }

        public async Task<Response> Handle(Query request, CancellationToken cancellationToken)
        {
            var response = await _mediator.Send(new Send.Request<Response>(
                HttpMethod: HttpMethod.Get,
                Path: "users",
                UriOptions: uriBuilder =>
                {
                    var queryParams = new List<string>();

                    if (request.Ids is {} ids)
                        queryParams.AddRange(ids.Select(x => $"id={Uri.EscapeDataString(x)}"));
                    if (request.Logins is {} logins)
                        queryParams.AddRange(logins.Select(x => $"login={Uri.EscapeDataString(x)}"));

                    uriBuilder.Query = string.Join('&', queryParams);
                },
                Token: request.Token,
                ClientId: _options.ClientId,
                RequestOptions: null
            ), cancellationToken);

            return response;
        }
    }
}

