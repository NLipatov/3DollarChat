#nullable disable
namespace EthachatShared.Models.Users
{
    public record IsUserExistDto
    {
        public string Username { get; set; }
        public bool IsExist { get; set; }
    }
}
