// 1. REQUISITO OBBLIGATORIO PWA: Evento Fetch
// Questo evento è necessario per sbloccare il tasto "Installa" su Chrome/Android.
self.addEventListener('fetch', function (event) {
    // Lasciamo che la richiesta proceda normalmente tramite la rete
    event.respondWith(fetch(event.request));
});

// 2. RICEZIONE NOTIFICHE PUSH
self.addEventListener('push', function (event) {
    if (event.data) {
        const dataText = event.data.text();

        const options = {
            body: dataText,
            icon: '/Img/icon-192.png',
            badge: '/Img/icon-192.png',
            vibrate: [100, 50, 100], // Vibrazione del telefono
            data: {
                dateOfArrival: Date.now(),
                primaryKey: '2'
            }
        };

        // Mostra la notifica sul telefono/PC
        event.waitUntil(
            self.registration.showNotification('ONUS Alert', options)
        );
    }
});

// 3. CLICK SULLA NOTIFICA
self.addEventListener('notificationclick', function (event) {
    // Chiude la notifica quando ci clicchi
    event.notification.close();

    // Apre l'app o porta l'utente alla home
    event.waitUntil(
        clients.matchAll({ type: 'window' }).then(function (clientList) {
            for (let i = 0; i < clientList.length; i++) {
                let client = clientList[i];
                if (client.url === '/' && 'focus' in client) {
                    return client.focus();
                }
            }
            if (clients.openWindow) {
                return clients.openWindow('/');
            }
        })
    );
});