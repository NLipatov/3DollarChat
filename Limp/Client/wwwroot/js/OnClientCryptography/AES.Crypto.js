async function GenerateAESKeyAsync() {
    let key = await window.crypto.subtle.generateKey
    (
        {
            name: "AES-GCM",
            length: 256
        },
        true,
        ["encrypt", "decrypt"]
    )
    const exportedKey = await window.crypto.subtle.exportKey(
        "raw",
        key
    );
    const exportedKeyBuffer = new Uint8Array(exportedKey);
    const exportedKeyBufferString = ab2str(exportedKeyBuffer);

    return exportedKeyBufferString;
}

function GenerateAESKeyForHLS(videoId) {
    (async () => {
        try {
            const key = await window.crypto.subtle.generateKey(
                {
                    name: "AES-CBC",
                    length: 128
                },
                true,
                ["encrypt", "decrypt"]
            );

            const exportedKey = await window.crypto.subtle.exportKey("raw", key);

            const exportedKeyBuffer = new Uint8Array(exportedKey);
            const hexKey = ab2hexstr(exportedKeyBuffer);

            console.log(hexKey);

            DotNet.invokeMethodAsync("Ethachat.Client", "OnHlsKeyReady", hexKey, videoId);
        } catch (error) {
            passErrorToDotNet("AES.Crypto.js", `Could not generate a key: ${error.toString()}`);
        }
    })();
}

function GenerateIVForHLS(videoId) {
    const ivLength = 16;
    const iv = window.crypto.getRandomValues(new Uint8Array(ivLength));
    return ab2hexstr(iv);
}

async function AESEncryptText(message, key) {
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

    return {
        ciphertext: ab2str(ciphertext),
        iv: ab2str(iv)
    };
}

//message, iv, key are a strings
async function AESDecryptText(message, key, iv) {
    const cypherText = new TextDecoder().decode(await window.crypto.subtle.decrypt(
        {
            name: "AES-GCM",
            iv: str2ab(iv)
        },
        await importSecretKey(key),
        str2ab(message))
    )

    return {
        ciphertext: cypherText,
        iv: iv
    };
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

function importSecretKey(ArrayBufferKeyString) {
    return window.crypto.subtle.importKey(
        "raw",
        str2ab(ArrayBufferKeyString),
        "AES-GCM",
        true,
        ["encrypt", "decrypt"]
    );
}