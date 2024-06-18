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

function ab2hexstr(buf) {
    return Array.from(new Uint8Array(buf))
        .map(byte => byte.toString(16).padStart(2, '0'))
        .join('');
}