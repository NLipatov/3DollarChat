namespace EthachatShared.Constants;

public record HubRelativeAddresses
{
    public static readonly string UsersHubRelativeAddress = "/usersHub";
    public static readonly string MessageHubRelativeAddress = "/messageDispatcherHub";
    public static readonly string AuthHubRelativeAddress = "/authHub";
    public static readonly string ExceptionLoggingHubRelativeAddress = "/loggingHub";
}