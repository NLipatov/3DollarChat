namespace ClientServerCommon.Models.Message
{
    public class Cryptogramm
    {
        public string? IV { get; set; }
        public string? Cyphertext { get; set; }
        public string? PlainText { get; set; }
    }
}
