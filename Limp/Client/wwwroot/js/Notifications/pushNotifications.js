(function () {
    const applicationServerPublicKey = 'BFICFdeXtnQqtoVo5XkFFBTr7VNvgoXrfb9Qp_O3PDAUG5TR7gnsvYt9hWSvZHQ24ZJ2y4kV4P7LfJ1akYV0Nb8';
    const baseUrl = window.location.origin;
    console.log('Base URL:', baseUrl);
    window.blazorPushNotifications = {
        requestSubscription: async () => {
            const worker = await navigator.serviceWorker.getRegistration();
            if (worker) {
                const existingSubscription = await worker.pushManager.getSubscription();
                if (!existingSubscription) {
                    const newSubscription = await subscribe(worker);
                    if (newSubscription) {
                        return {
                            url: newSubscription.endpoint,
                            p256dh: arrayBufferToBase64(newSubscription.getKey('p256dh')),
                            auth: arrayBufferToBase64(newSubscription.getKey('auth'))
                        };
                    }
                }
            }
        }
    };


    async function subscribe(worker) {
        console.log("sever key in subscribe: " + applicationServerPublicKey);
        try {
            return await worker.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: applicationServerPublicKey
            });
        } catch (error) {
            if (error.name === 'NotAllowedError') {
                return null;
            }
            throw error;
        }
    }

    function arrayBufferToBase64(buffer) {
        // https://stackoverflow.com/a/9458996
        var binary = '';
        var bytes = new Uint8Array(buffer);
        var len = bytes.byteLength;
        for (var i = 0; i < len; i++) {
            binary += String.fromCharCode(bytes[i]);
        }
        return window.btoa(binary);
    }
})();