using System.Text.Json.Serialization;
using EthachatShared.Models.Authentication.Enums;

namespace EthachatShared.Models.Authentication.Models.UserAuthentication
{
    public class UserAuthenticationOperationResult
    {
        [JsonPropertyName("systemMessage")]
        public string? SystemMessage { get; set; }
        [JsonPropertyName("userDTO")]
        public UserAuthentication? UserDto { get; set; }
        [JsonPropertyName("result")]
        public OperationResultType ResultType { get; set; }
    }
}
