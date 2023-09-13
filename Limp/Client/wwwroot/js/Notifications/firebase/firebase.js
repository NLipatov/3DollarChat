import { initializeApp } from "https://www.gstatic.com/firebasejs/10.3.1/firebase-app.js";
import { getMessaging, getToken } from "https://www.gstatic.com/firebasejs/10.3.1/firebase-messaging.js";

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
getToken(messaging, {vapidKey: "BA6mK_HXP2I9vXg6e4r2t_3wFwkhCh6l2THvFPqrPb1ERENvFN82VDk4pKnoHMxsd6oKGrTccX_0aLCDDFmXH00"});

Notification.requestPermission().then(function (permission) {
    if (permission === "granted") {
        console.log("Permission granted.");
        getToken(messaging, { vapidKey: 'BA6mK_HXP2I9vXg6e4r2t_3wFwkhCh6l2THvFPqrPb1ERENvFN82VDk4pKnoHMxsd6oKGrTccX_0aLCDDFmXH00' }).then(function (currentToken) {
            if (currentToken) {
                console.log("Current Token:", currentToken);
                // Отправьте полученный токен на ваш сервер и обновите UI, если необходимо
            } else {
                console.log('No registration token available.');
            }
        }).catch(function (err) {
            console.error("Error retrieving token:", err);
        });
    } else {
        console.log("Permission denied.");
    }
}).catch(function (error) {
    console.error("Error requesting notification permission:", error);
});