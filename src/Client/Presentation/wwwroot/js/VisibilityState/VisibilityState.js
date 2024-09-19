addEventListener("visibilitychange", (event) => {
    DotNet.invokeMethodAsync("Ethachat.Client", "OnVisibilityChange", document.visibilityState === "visible");
});