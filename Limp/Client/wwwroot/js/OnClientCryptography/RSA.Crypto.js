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
        await exportPublicKeyToDotnet(keyPair.publicKey);
        await exportPrivateKeyToDotnet(keyPair.privateKey);
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

    DotNet.invokeMethodAsync("Limp.Client", "OnKeyExtracted", pemExported, 2, 1, null);
}

/*
Export the given key and write it into the "exported-key" space.
*/
const exportPrivateKeyToDotnet = async (key) =>
{
    const exported = await window.crypto.subtle.exportKey(
        "pkcs8",
        key
    );
    const exportedAsString = ab2str(exported);
    const exportedAsBase64 = window.btoa(exportedAsString);
    const pemExported = `-----BEGIN PRIVATE KEY-----\n${exportedAsBase64}\n-----END PRIVATE KEY-----`;

    DotNet.invokeMethodAsync("Limp.Client", "OnKeyExtracted", pemExported, 1, 2, null);
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
function importPublicKey(pem)
{
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

async function importPrivateKey(pkcs8Pem)
{
    return await window.crypto.subtle.importKey(
        "pkcs8",
        getPkcs8Der(pkcs8Pem),
        {
            name: "RSA-OAEP",
            hash: "SHA-256",
        },
        true,
        ["decrypt"]
    );
}

function getPkcs8Der(pkcs8Pem)
{
    const pemHeader = "-----BEGIN PRIVATE KEY-----";
    const pemFooter = "-----END PRIVATE KEY-----";
    var pemContents = pkcs8Pem.substring(pemHeader.length, pkcs8Pem.length - pemFooter.length);
    var binaryDerString = window.atob(pemContents);
    return str2ab(binaryDerString);
}

/*
Get the encoded message, encrypt it and display a representation
of the ciphertext in the "Ciphertext" element.
*/
async function encryptMessage(message, key) {
    const encoded = new TextEncoder().encode(message);
    const ciphertext = await window.crypto.subtle.encrypt(
        {
            name: "RSA-OAEP"
        },
        key,
        encoded
    );

    console.log(ciphertext);
    return ab2str(ciphertext);
}

async function decryptMessage(ciphertext, key) {
    let decrypted = await window.crypto.subtle.decrypt(
        {
            name: "RSA-OAEP"
        },
        key,
        await str2ab(ciphertext)
    );
    return new TextDecoder().decode(decrypted);
}

async function EncryptWithRSAPublicKey(message, RSApublicKey) {
    encryptionKey = await importPublicKey(RSApublicKey);
    return await encryptMessage(message, encryptionKey);
}

async function DecryptWithRSAPrivateKey(ciphertext, privateKey) {
    decryptionKey = await importPrivateKey(privateKey);
    return await decryptMessage(ciphertext, decryptionKey);
}