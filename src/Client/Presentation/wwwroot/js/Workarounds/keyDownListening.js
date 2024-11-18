document.addEventListener('keydown', (event) => {
    DotNet.invokeMethodAsync("Ethachat.Client", "OnKeyDown", event.code);
});