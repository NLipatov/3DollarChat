namespace EthachatShared.Models.ConnectedUsersManaging
{
    public record UserConnectionsReport
    {
        /// <summary>
        /// Datetime when this DTO was formed at
        /// </summary>
        public DateTime FormedAt { get; set; }
        /// <summary>
        /// Collection of user connections at the time of forming this DTO
        /// </summary>
        public UserConnection[] UserConnections { get; set; } = new UserConnection[0];
    }
}
