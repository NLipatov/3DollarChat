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

async function AESEncryptData(data, key) {
    const initializationVector = crypto.getRandomValues(new Uint8Array(12));
    
    const encryptedDataArrayBuffer = await crypto.subtle.encrypt(
        {
            name: 'AES-GCM',
            iv: initializationVector,
        },
        await importSecretKey(key),
        data
    );

    const encryptedData = new Uint8Array(encryptedDataArrayBuffer);

    // Create a new Uint8Array to hold IV length, IV and encrypted data
    const resultArray = new Uint8Array(1 + initializationVector.length + encryptedData.length);

    // Set IV length as the first byte
    resultArray[0] = initializationVector.length;

    // Set IV after the first byte
    resultArray.set(initializationVector, 1);

    // Set encrypted data after the IV
    resultArray.set(encryptedData, 1 + initializationVector.length);

    return resultArray;
}

async function AESDecryptData(data, key, iv) {
    const decryptedDataArrayBuffer = await crypto.subtle.decrypt(
        {
            name: 'AES-GCM',
            iv: iv,
        },
        await importSecretKey(key),
        data
    );

    const decryptedData = new Uint8Array(decryptedDataArrayBuffer);

    return decryptedData;
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