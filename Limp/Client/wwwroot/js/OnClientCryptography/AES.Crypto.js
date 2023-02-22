function ab2str(buf) {
    return String.fromCharCode.apply(null, new Uint8Array(buf));
}

//Convert a string into an ArrayBuffer
//from https://developers.google.com/web/updates/2012/06/How-to-convert-ArrayBuffer-to-and-from-String
function str2ab(str) {
    const buf = new ArrayBuffer(str.length);
    const bufView = new Uint8Array(buf);
    for (let i = 0, strLen = str.length; i < strLen; i++) {
        bufView[i] = str.charCodeAt(i);
    }
    return buf;
}

let AESKey;
let iv;

function GenerateAESKey() {
    window.crypto.subtle.generateKey
        (
            {
                name: "AES-GCM",
                length: 256
            },
            true,
            ["encrypt", "decrypt"]
    ).then(async (Key) => {
            AESKey = Key;
            await exportAESKeyToDotnet(AESKey);
        });
}

const exportAESKeyToDotnet = async (key) => {
    const exported = await window.crypto.subtle.exportKey(
        "raw",
        key
    );
    const exportedKeyBuffer = new Uint8Array(exported);
    const exportedKeyBufferString = ab2str(exportedKeyBuffer);

    DotNet.invokeMethodAsync("Limp.Client", "OnAESKeyExpot", exportedKeyBufferString);
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

async function AESEncryptMessage(message) {
    const encoded = new TextEncoder().encode(message);
    // The iv must never be reused with a given key.
    iv = window.crypto.getRandomValues(new Uint8Array(12));
    const ciphertext = await window.crypto.subtle.encrypt(
        {
            name: "AES-GCM",
            iv: iv
        },
        AESKey,
        encoded
    );

    console.log(ciphertext);
    return ab2str(ciphertext);
}

async function AESDecryptMessage(message) {
    return new TextDecoder().decode(await window.crypto.subtle.decrypt(
        {
            name: "AES-GCM",
            iv: iv
        },
        AESKey,
        str2ab(message))
    )
}