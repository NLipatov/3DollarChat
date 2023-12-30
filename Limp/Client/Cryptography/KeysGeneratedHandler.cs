namespace Ethachat.Client.Cryptography
{
    public static class KeysGeneratedHandler
    {
        public static List<Action> OnKeysGenerated { get; set; } = new();

        public static void SubscribeToRSAKeysGeneratedEvent(Action action)
        {
            OnKeysGenerated.Add(action);
        }

        public static void CallOnKeysGenerated()
        {
            foreach (var action in OnKeysGenerated)
            {
                action();
            }
        }
    }
}
