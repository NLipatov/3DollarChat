namespace ClientServerCommon.Models.HubMessages
{
    public record UsersOnlineMessage
    {
        public DateTime FormedAt { get; set; }
        public UserConnection[] UserConnections { get; set; } = new UserConnection[0];
    }
}
