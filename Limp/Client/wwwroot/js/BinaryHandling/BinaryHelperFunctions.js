function createBlobUrl(byteArray, mimeType) {
    var blob = new Blob([byteArray], { type: mimeType });
    return URL.createObjectURL(blob);
}