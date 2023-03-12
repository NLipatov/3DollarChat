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

    DotNet.invokeMethodAsync("Limp.Client", "OnKeyExtracted", exportedKeyBufferString, 3, 3, contactName);
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

async function AESEncryptMessage(message, key) {
    const encoded = new TextEncoder().encode(message);
    // The iv must never be reused with a given key.
    iv = window.crypto.getRandomValues(new Uint8Array(12));
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

function ExportIV() {
    console.log(`returning as iv: ${ab2str(iv)}`);
    return ab2str(iv);
}

function ImportIV(ivArrayBufferAsString) {
    iv = str2ab(ivArrayBufferAsString);
}

async function AESDecryptMessage(message, key) {
    return new TextDecoder().decode(await window.crypto.subtle.decrypt(
        {
            name: "AES-GCM",
            iv: iv
        },
        await importSecretKey(key),
        str2ab(message))
    )
}