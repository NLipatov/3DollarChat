function passErrorToDotNet(scriptName, error) {
    DotNet.invokeMethodAsync("Ethachat.Client", "OnJsException", scriptName, error.toString());
}