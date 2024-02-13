let iv;

function GenerateAESKey(contactName) {
    window.crypto.subtle.generateKey
        (
            {
                name: "AES-GCM",
                length: 256
            },
            true,
            ["encrypt", "decrypt"]
    ).then(async (Key) => {
        await exportAESKeyToDotnet(Key, contactName);
        });
}

const exportAESKeyToDotnet = async (key, contactName) => {
    const exported = await window.crypto.subtle.exportKey(
        "raw",
        key
    );
    const exportedKeyBuffer = new Uint8Array(exported);
    const exportedKeyBufferString = ab2str(exportedKeyBuffer);

    DotNet.invokeMethodAsync("Ethachat.Client", "OnKeyExtracted", exportedKeyBufferString, 3, 3, contactName);
}

function importSecretKey(ArrayBufferKeyString) {
    return window.crypto.subtle.importKey(
        "raw",
        str2ab(ArrayBufferKeyString),
        "AES-GCM",
        true,
        ["encrypt", "decrypt"]
    );
}

async function AESEncryptText(message, key) {
    const encoded = new TextEncoder().encode(message);
    // The iv must never be reused with a given key.
    iv = window.crypto.getRandomValues(new Uint8Array(12));
    ivDictionary[message] = iv;
    const ciphertext = await window.crypto.subtle.encrypt(
        {
            name: "AES-GCM",
            iv: iv
        },
        await importSecretKey(key),
        encoded
    );

    return ab2str(ciphertext);
}

async function AESDecryptText(message, key) {
    return new TextDecoder().decode(await window.crypto.subtle.decrypt(
        {
            name: "AES-GCM",
            iv: ivDictionary[message]
        },
        await importSecretKey(key),
        str2ab(message))
    )
}

async function AESEncryptData(base64String, key) {
    const binaryData = atob(base64String);
    const encodedData = new TextEncoder().encode(binaryData);

    const encryptedDataArrayBuffer = await crypto.subtle.encrypt(
        {
            name: 'AES-GCM',
            iv: iv,
        },
        await importSecretKey(key),
        encodedData
    );

    const encryptedData = new Uint8Array(encryptedDataArrayBuffer);
    const encryptedBase64String = btoa(String.fromCharCode.apply(null, encryptedData));

    return encryptedBase64String;
}

async function AESDecryptData(encryptedBase64String, key) {
    const encryptedData = new Uint8Array(Array.from(atob(encryptedBase64String)).map(char => char.charCodeAt(0)));

    const decryptedDataArrayBuffer = await crypto.subtle.decrypt(
        {
            name: 'AES-GCM',
            iv: iv,
        },
        await importSecretKey(key),
        encryptedData
    );

    const decryptedData = new Uint8Array(decryptedDataArrayBuffer);
    const decryptedBase64String = btoa(String.fromCharCode.apply(null, decryptedData));

    return decryptedBase64String;
}

let ivDictionary = {};
function ImportIV(ivArrayBufferAsString, key) {
    ivDictionary[key] = str2ab(ivArrayBufferAsString);
}

function ExportIV(key) {
    return ab2str(ivDictionary[key]);
}

function DeleteIv(key) {
    delete ivDictionary[key];
}