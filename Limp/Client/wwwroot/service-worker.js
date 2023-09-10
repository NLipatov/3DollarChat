// In development, always fetch from the network and do not enable offline support.
// This is because caching would make development more difficult (changes would not
// be reflected on the first load after each change).
self.addEventListener('fetch', () => { });

self.addEventListener('install', async event => {
    console.log('Installing service worker...');
    self.skipWaiting();
});

self.addEventListener('push', event => {
    const payload = event.data.json();

    event.waitUntil(
        self.clients.matchAll({ type: 'window', includeUncontrolled: true })
            .then(clients => {
                if (clients && clients.length === 0) {
                    //if there is no open clients, we will show notification
                    return self.registration.showNotification('η Chat', {
                        body: payload.message,
                        icon: 'icon-512.png',
                        vibrate: [100, 50, 100],
                        data: { url: payload.url }
                    });
                }
            })
    );
});

self.addEventListener('notificationclick', event => {
    event.notification.close();
    event.waitUntil(clients.openWindow(event.notification.data.url));
});