namespace Ethachat.Client.Cryptography
{
    public static class KeysGeneratedHandler
    {
        public static List<Action> OnKeysGenerated { get; } = new();

        public static void SubscribeToRsaKeysGeneratedEvent(Action action) => OnKeysGenerated.Add(action);

        public static void CallOnKeysGenerated() => OnKeysGenerated.ForEach(x => x());
    }
}
