using Refit;
using Teraa.Twitch.Helix.Models;

namespace Teraa.Twitch.Helix;

public interface IHelixApi
{
    [Get("/users")]
    Task<Response<User>> GetUser(
        IReadOnlyList<string>? id = null,
        IReadOnlyList<string>? login = null
    );
}
