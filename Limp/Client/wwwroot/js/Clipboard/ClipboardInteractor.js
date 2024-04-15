"use strict";

function AddOnPasteEvent() {
    document.addEventListener('paste', (event) => {
        handlePaste(event);
    });
}

function RemoveOnPasteEvent() {
    document.removeEventListener('paste', handlePaste);
}

async function handlePaste(event) {
    console.log("handlePaste");
    const clipboardData = (event.clipboardData || window.clipboardData);
    console.log(clipboardData);
    const files = Array.from(clipboardData.files);
    console.log(files);
    
    files.forEach(file => {
        const fileName = file.name;
        console.log(fileName);
        const reader = new FileReader();

        reader.onload = function(event) {
            const arrayBuffer = event.target.result;
            const byteArray = new Uint8Array(arrayBuffer);


            DotNet.invokeMethodAsync("Ethachat.Client", "FileFromClipboard", byteArray, fileName);
        };

        reader.readAsArrayBuffer(file);
    });
}

export {
    AddOnPasteEvent,
    RemoveOnPasteEvent,
    handlePaste
}