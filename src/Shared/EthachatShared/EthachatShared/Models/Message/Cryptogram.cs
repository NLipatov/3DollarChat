namespace EthachatShared.Models.Message
{
    public class Cryptogram
    {
        public string? Iv { get; set; }
        public string? Cyphertext { get; set; }
        public Guid KeyId { get; set; }
    }
}
