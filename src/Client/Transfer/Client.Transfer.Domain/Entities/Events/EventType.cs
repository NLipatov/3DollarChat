namespace Client.Transfer.Domain.Entities.Events;

public enum EventType
{
    Unset,
    OnTyping,
    MessageReceived,
    MessageRead,
    ResendRequest,
    ConversationDeletion,
    AesOfferAccepted,
    RsaPubKeyRequest,
    DataTransferConfirmation
}