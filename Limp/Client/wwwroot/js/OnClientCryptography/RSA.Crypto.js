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

let cryptoKeyPair;

function GenerateRSAOAEPKeyPair()
{
    window.crypto.subtle.generateKey
    (
        {
            name: "RSA-OAEP",
            modulusLength: 4096,
            publicExponent: new Uint8Array([1, 0, 1]),
            hash: "SHA-256",
        },
        true,
        ["encrypt", "decrypt"]
    ).then(async(keyPair) =>
    {
        cryptoKeyPair = keyPair;
        await exportPublicKeyToDotnet(keyPair.publicKey, "publicKey");
    });
}

const exportPublicKeyToDotnet = async (key) =>
{
    const exported = await window.crypto.subtle.exportKey(
        "spki",
        key
    );
    const exportedAsString = ab2str(exported);
    const exportedAsBase64 = window.btoa(exportedAsString);
    const pemExported = `-----BEGIN PUBLIC KEY-----\n${exportedAsBase64}\n-----END PUBLIC KEY-----`;

    DotNet.invokeMethodAsync("Limp.Client", "OnPublicKeyExtracted", pemExported);
}

async function decryptMessage(message) {
    return new TextDecoder().decode(await window.crypto.subtle.decrypt(
        {
            name: "RSA-OAEP"
        },
        cryptoKeyPair.privateKey,
        str2ab(message))
    )
}

/*
The unwrapped signing key.
*/
let encryptionKey;
/*
Import a PEM encoded RSA public key, to use for RSA-OAEP encryption.
Takes a string containing the PEM encoded key, and returns a Promise
that will resolve to a CryptoKey representing the public key.
*/
function importPublicKey(pem) {
    // fetch the part of the PEM string between header and footer
    const pemHeader = "-----BEGIN PUBLIC KEY-----";
    const pemFooter = "-----END PUBLIC KEY-----";
    const pemContents = pem.substring(pemHeader.length, pem.length - pemFooter.length);
    // base64 decode the string to get the binary data
    const binaryDerString = window.atob(pemContents);
    // convert from a binary string to an ArrayBuffer
    const binaryDer = str2ab(binaryDerString);

    return window.crypto.subtle.importKey(
        "spki",
        binaryDer,
        {
            name: "RSA-OAEP",
            hash: "SHA-256"
        },
        true,
        ["encrypt"]
    );
}

/*
Get the encoded message, encrypt it and display a representation
of the ciphertext in the "Ciphertext" element.
*/
async function encryptMessage(message) {
    const encoded = new TextEncoder().encode(message);
    const ciphertext = await window.crypto.subtle.encrypt(
        {
            name: "RSA-OAEP"
        },
        encryptionKey,
        encoded
    );

    console.log(ciphertext);
    return ab2str(ciphertext);
}

async function EncryptWithRSAPublicKey(message, RSApublicKey) {
    encryptionKey = await importPublicKey(RSApublicKey);
    return await encryptMessage(message);
}