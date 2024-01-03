﻿using Ethachat.Client.Cryptography;
using Ethachat.Client.Cryptography.CryptoHandlers.Handlers;
using Ethachat.Client.Cryptography.KeyStorage;
using Ethachat.Client.Services.BrowserKeyStorageService;
using EthachatShared.Encryption;
using EthachatShared.Models.Message;

namespace Ethachat.Client.HubConnectionManagement.ConnectionHandlers.MessageDispatcher.AESOfferHandling
{
    public class AESOfferHandler : IAESOfferHandler
    {
        private readonly ICryptographyService _cryptographyService;
        private readonly IBrowserKeyStorage _localKeyManager;

        public AESOfferHandler(ICryptographyService cryptographyService, IBrowserKeyStorage localKeyManager)
        {
            _cryptographyService = cryptographyService;
            _localKeyManager = localKeyManager;
        }
        public async Task<Message> GetAESOfferResponse(Message offerMessage)
        {
            string decryptedAESKey = await DecryptAESKeyFromMessage(offerMessage);

            Key aesKeyForConversation = new()
            {
                Value = decryptedAESKey,
                Contact = offerMessage.Sender,
                Format = KeyFormat.Raw,
                Type = KeyType.Aes,
                Author = offerMessage.Sender,
                IsAccepted = true
            };

            if (!string.IsNullOrWhiteSpace(offerMessage.Sender))
            {
                lock (InMemoryKeyStorage.AESKeyStorage)
                {
                    InMemoryKeyStorage.AESKeyStorage[offerMessage.Sender] = aesKeyForConversation;
                }

                await _localKeyManager.SaveInMemoryKeysInLocalStorage();
            }

            return new Message
            {
                Sender = offerMessage.TargetGroup,
                Type = MessageType.AesAccept,
                TargetGroup = offerMessage.Sender,
            };
        }

        private async Task<string> DecryptAESKeyFromMessage(Message message)
        {

            if (string.IsNullOrWhiteSpace(message.Sender))
                throw new ArgumentException($"AES offer was not containing a {nameof(Message.Sender)}");

            string decryptedAESKey = await GetDecryptedAESKeyFromMessage(message);

            return decryptedAESKey;
        }

        private async Task<string> GetDecryptedAESKeyFromMessage(Message message)
        {
            if (string.IsNullOrWhiteSpace(message.Cryptogramm?.Cyphertext))
                throw new ArgumentException
                    ($"Cannot get decrypted AES key from {typeof(Message).Name}: " +
                    $"{typeof(Message).Name} was not containing a {nameof(Message.Cryptogramm.Cyphertext)} property value.");

            string? encryptedAESKey = message.Cryptogramm.Cyphertext;
            if (string.IsNullOrWhiteSpace(encryptedAESKey))
                throw new ArgumentException("AESOffer message was not containing any AES Encrypted string.");

            var decryptedCryptogram = (await _cryptographyService.DecryptAsync<RSAHandler>
            (new Cryptogramm
            {
                Cyphertext = encryptedAESKey
            }));

            string? decryptedAESKey = decryptedCryptogram.Cyphertext;

            if (string.IsNullOrWhiteSpace(decryptedAESKey))
                throw new ArgumentException("Could not decrypt an AES Key.");

            return decryptedAESKey;
        }
    }
}
