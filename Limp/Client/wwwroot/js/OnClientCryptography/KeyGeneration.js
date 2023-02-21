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

function ab2str(buf) {
    return String.fromCharCode.apply(null, new Uint8Array(buf));
}

function str2ab(str) {
    const buf = new ArrayBuffer(str.length);
    const bufView = new Uint8Array(buf);
    for (let i = 0, strLen = str.length; i < strLen; i++) {
        bufView[i] = str.charCodeAt(i);
    }
    return buf;
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