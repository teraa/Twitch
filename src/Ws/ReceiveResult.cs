namespace Teraa.Twitch.Ws;

public readonly struct ReceiveResult
{
    public ReceiveResult(bool isClose, string? message)
    {
        IsClose = isClose;
        Message = message;
    }

    public static ReceiveResult Close { get; } = new(true, null);

    public bool IsClose { get; }
    public string? Message { get; }
}
