using EthachatShared.Models.Authentication.Enums;
using EthachatShared.Models.Authentication.Models.Credentials.Implementation;

namespace EthachatShared.Models.Authentication.Models
{
    public class TokenRelatedOperationResult
    {
        public OperationResultType ResultType { get; set; }
        public FailureType? FailureType { get; set; }
        public JwtPair? JwtPairPayload { get; set; }
        public string? Username { get; set; }
        public string? Message { get; set; }
    }
}
