import { initializeApp } from "https://www.gstatic.com/firebasejs/10.3.1/firebase-app.js";
import { getMessaging, getToken } from "https://www.gstatic.com/firebasejs/10.3.1/firebase-messaging.js";

window.getFCMToken = async () => {
    try {
        let token = await requestUserForNotificationPermission();
        return token;
    } catch (error) {
        console.error("Error in GetToken:", error);
        throw error;
    }
}

const requestUserForNotificationPermission = () => {
    return new Promise(async (resolve, reject) => {
        try {
            const permission = await Notification.requestPermission();
            if (permission === "granted") {
                console.log("Permission granted.");
                const token = await getFCMToken();
                if (token) {
                    console.log("Found a registration token to send to backend.")
                    resolve(token);
                } else {
                    console.log('No registration token available.');
                    reject('No registration token available.');
                }
            } else {
                console.log("Permission denied.");
                reject('Permission denied.');
            }
        } catch (error) {
            console.error("Error requesting notification permission:", error);
            reject(error);
        }
    });
}

async function getFCMToken() {
    const config = {
        apiKey: "AIzaSyCexghrqeFwvGqjaTe3JIwUTrxUc2X-bdw",
        authDomain: "ethachat-2023.firebaseapp.com",
        projectId: "ethachat-2023",
        storageBucket: "ethachat-2023.appspot.com",
        messagingSenderId: "383190008660",
        appId: "1:383190008660:web:b9045fa8b74f2ab969fec1"
    };

    initializeApp(config);
    const messaging = getMessaging();
    const currentToken = await getToken(messaging, { vapidKey: "BA6mK_HXP2I9vXg6e4r2t_3wFwkhCh6l2THvFPqrPb1ERENvFN82VDk4pKnoHMxsd6oKGrTccX_0aLCDDFmXH00" });
    return currentToken;
}

window.isServiceWorkerInstalled = () => {
    return navigator.serviceWorker.getRegistration().then(function (registration) {
        return registration !== undefined;
    });
}

window.subscribeToServiceWorkerInstallEvent = (callback) => {
    navigator.serviceWorker.addEventListener('controllerchange', function () {
        callback();
    });
}