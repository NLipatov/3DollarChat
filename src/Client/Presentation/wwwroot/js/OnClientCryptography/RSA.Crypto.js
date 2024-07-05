function convertPemToBinary(pem) {
    const lines = pem.split('\n');
    let encoded = '';
    for (let i = 0; i < lines.length; i++) {
        if (lines[i].trim().length > 0 && !lines[i].includes('-----BEGIN') && !lines[i].includes('-----END')) {
            encoded += lines[i].trim();
        }
    }
    const binary = new Uint8Array(window.atob(encoded).split("").map(char => char.charCodeAt(0)));
    return binary.buffer;
}

// Key generation and conversion functions
async function GenerateRSAOAEPKeyPairAsync() {
    try {
        const keyPair = await window.crypto.subtle.generateKey(
            {
                name: "RSA-OAEP",
                modulusLength: 4096,
                publicExponent: new Uint8Array([1, 0, 1]),
                hash: "SHA-256",
            },
            true,
            ["encrypt", "decrypt"]
        );

        const publicKeyPem = await publicToPemAsync(keyPair.publicKey);
        const privateKeyPem = await privateToPemAsync(keyPair.privateKey);

        return [publicKeyPem, privateKeyPem];
    } catch (error) {
        console.error('Error generating RSA-OAEP key pair:', error);
        throw error;
    }
}

const publicToPemAsync = async (key) => {
    const exported = await window.crypto.subtle.exportKey(
        "spki",
        key
    );
    const exportedAsString = ab2str(exported);
    const exportedAsBase64 = window.btoa(exportedAsString);
    const pemExported = `-----BEGIN PUBLIC KEY-----\n${exportedAsBase64}\n-----END PUBLIC KEY-----`;

    return pemExported;
}

const privateToPemAsync = async (key) => {
    const exported = await window.crypto.subtle.exportKey(
        "pkcs8",
        key
    );
    const exportedAsString = ab2str(exported);
    const exportedAsBase64 = window.btoa(exportedAsString);
    const pemExported = `-----BEGIN PRIVATE KEY-----\n${exportedAsBase64}\n-----END PRIVATE KEY-----`;

    return pemExported;
}

// Import PEM encoded keys
async function importPublicKey(pem) {
    const binaryDer = convertPemToBinary(pem);
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

async function importPrivateKey(pem) {
    const binaryDer = convertPemToBinary(pem);
    return window.crypto.subtle.importKey(
        "pkcs8",
        binaryDer,
        {
            name: "RSA-OAEP",
            hash: "SHA-256",
        },
        true,
        ["decrypt"]
    );
}

// Encryption and Decryption functions
async function EncryptWithRSAPublicKey(message, RSApublicKey) {
    const encodedMessage = new TextEncoder().encode(message);
    const key = await importPublicKey(RSApublicKey);
    const ciphertext = await window.crypto.subtle.encrypt(
        {
            name: "RSA-OAEP"
        },
        key,
        encodedMessage
    );
    return new Uint8Array(ciphertext);
}

async function DecryptWithRSAPrivateKey(ciphertext, privateKey) {
    const key = await importPrivateKey(privateKey);
    const decryptedData = await window.crypto.subtle.decrypt(
        {
            name: "RSA-OAEP"
        },
        key,
        ciphertext
    );
    return new TextDecoder().decode(decryptedData);
}

// New function to encrypt Uint8Array data
async function EncryptDataWithRSAPublicKey(data, RSApublicKey) {
    try {
        const key = await importPublicKey(RSApublicKey);
        const ciphertext = await window.crypto.subtle.encrypt(
            {
                name: "RSA-OAEP"
            },
            key,
            data
        );
        return new Uint8Array(ciphertext);
    } catch (error) {
        console.error('An error occurred:', error.message);
        throw new Error('Failed to import public key. Please ensure the correct key is provided.');
    }
}

// New function to decrypt Uint8Array data
async function DecryptDataWithRSAPrivateKey(ciphertext, RSAprivateKey) {
    try {
        const key = await importPrivateKey(RSAprivateKey);
        const decryptedData = await window.crypto.subtle.decrypt(
            {
                name: "RSA-OAEP"
            },
            key,
            ciphertext
        );
        return new Uint8Array(decryptedData);
    } catch (error) {
        console.error('An error occurred:', error.message);
        throw new Error('Failed to import private key. Please ensure the correct key is provided.');
    }
}