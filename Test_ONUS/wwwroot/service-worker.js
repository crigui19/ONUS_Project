// Questo evento scatta quando il server C# manda una notifica
self.addEventListener('push', function (event) {
    let data = {};

    // Controlliamo se ci sono dati nel messaggio
    if (event.data) {
        data = event.data.json();
    }

    // Configura l'aspetto della notifica sul telefono
    const title = data.titolo || "ONUS Athletes";
    const options = {
        body: data.messaggio || "Hai una nuova notifica dal tuo allenatore.",
        icon: '/Img/icon-192.png', // L'icona della tua app
        badge: '/Img/icon-192.png', // L'iconcina piccola nella barra di stato
        vibrate: [200, 100, 200, 100, 200], // Vibrazione stile messaggio
        data: {
            url: data.url || '/' // Dove va se clicca la notifica
        }
    };

    // Mostra la notifica sullo schermo!
    event.waitUntil(
        self.registration.showNotification(title, options)
    );
});

// Cosa succede quando l'atleta tocca la notifica col dito
self.addEventListener('notificationclick', function (event) {
    event.notification.close();
    event.waitUntil(
        clients.openWindow(event.notification.data.url)
    );
});