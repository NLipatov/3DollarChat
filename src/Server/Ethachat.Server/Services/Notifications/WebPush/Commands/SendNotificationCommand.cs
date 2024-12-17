namespace Ethachat.Server.Services.Notifications.WebPush.Commands;

public record SendNotificationCommand
{
    public string PushMessage { get; set; } = string.Empty;
    public bool IsSendRequired { get; set; }
}