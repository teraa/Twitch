namespace Teraa.Twitch.Tmi;

public readonly struct ReceiveResult
{
    public ReceiveResult(bool isClose, string? message)
    {
        IsClose = isClose;
        Message = message;
    }

    public bool IsClose { get; }
    public string? Message { get; }
}
